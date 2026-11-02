using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine.Journal;
using WindowsBox.Core.Platform;

/// <summary>
/// Short-lived elevated helper. Launched per apply/rollback batch via UAC, exits when done.
/// Never phones home. Never stays resident. AOT-friendly JSON via source generation.
/// </summary>
return ElevatorHost.Run(args);

internal static class ElevatorHost
{
    public static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine(
                """
                WindowsBox.Elevator — short-lived admin helper (no resident service).

                Usage:
                  WindowsBox.Elevator --ping
                  WindowsBox.Elevator --apply-plan <plan.json>
                  WindowsBox.Elevator --rollback-journal <journal.json>

                Speaks JSON on stdout. Never phones home. Exits when done.
                """);
            return 0;
        }

        try
        {
            return args[0] switch
            {
                "--ping" => Ping(),
                "--apply-plan" => RequireAdminThen(() => ApplyPlan(RequirePath(args, 1))),
                "--rollback-journal" => RequireAdminThen(() => Rollback(RequirePath(args, 1))),
                _ => Fail($"Unknown argument: {args[0]}"),
            };
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    /// <summary>
    /// Mutating commands require an elevated admin token on Windows.
    /// Non-Windows hosts (CI) are allowed for dry acknowledgment only.
    /// </summary>
    private static int RequireAdminThen(Func<int> action)
    {
        if (OperatingSystem.IsWindows() && !IsElevated())
        {
            return Fail(
                "Refusing to mutate services: Elevator is not running elevated. " +
                "Relaunch via UAC (ElevatorClient) or an elevated shell. Non-admins cannot apply or rollback.");
        }

        return action();
    }

    private static string RequirePath(string[] args, int index)
    {
        if (args.Length <= index)
        {
            throw new ArgumentException("Missing path argument.");
        }

        return args[index];
    }

    private static int Ping()
    {
        Write(new ElevatorResponse
        {
            Ok = true,
            Message = "pong",
            Elevated = IsElevated(),
            Platform = Environment.OSVersion.VersionString,
        });
        return 0;
    }

    private static int ApplyPlan(string path)
    {
        if (!File.Exists(path))
        {
            return Fail($"Plan not found: {path}");
        }

        using var stream = File.OpenRead(path);
        var plan = JsonSerializer.Deserialize(stream, ElevatorJsonContext.Default.ElevatorPlan)
                   ?? throw new InvalidOperationException("Invalid plan JSON.");

        if (!OperatingSystem.IsWindows())
        {
            Write(new ElevatorResponse
            {
                Ok = true,
                Message = $"Non-Windows host: acknowledged plan '{plan.Id}' with {plan.Changes.Count} change(s); no SCM mutations.",
                Elevated = IsElevated(),
                Applied = 0,
                Skipped = plan.Changes.Count,
            });
            return 0;
        }

        return ApplyPlanWindows(plan);
    }

    [SupportedOSPlatform("windows")]
    private static int ApplyPlanWindows(ElevatorPlan plan)
    {
        var services = PlatformAdapters.CreateServiceController();
        var applied = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var change in plan.Changes)
        {
            if (!Enum.TryParse<ServiceStartType>(change.TargetStartType, ignoreCase: true, out var startType))
            {
                skipped++;
                errors.Add($"{change.ServiceName}: bad start type '{change.TargetStartType}'");
                continue;
            }

            try
            {
                if (!services.Exists(change.ServiceName))
                {
                    skipped++;
                    errors.Add($"{change.ServiceName}: not found");
                    continue;
                }

                services.SetStartType(change.ServiceName, startType, change.DelayedAutostart);
                applied++;
            }
            catch (Exception ex)
            {
                skipped++;
                errors.Add($"{change.ServiceName}: {ex.Message}");
            }
        }

        Write(new ElevatorResponse
        {
            Ok = errors.Count == 0,
            Message = errors.Count == 0
                ? $"Applied {applied} change(s) for plan '{plan.Id}'."
                : $"Applied {applied}, skipped {skipped}. " + string.Join("; ", errors),
            Elevated = IsElevated(),
            Applied = applied,
            Skipped = skipped,
        });
        return errors.Count == 0 ? 0 : 1;
    }

    private static int Rollback(string path)
    {
        if (!File.Exists(path))
        {
            return Fail($"Journal not found: {path}");
        }

        using var stream = File.OpenRead(path);
        var journal = JsonSerializer.Deserialize(stream, ElevatorJsonContext.Default.ApplyJournal)
                      ?? throw new InvalidOperationException("Invalid journal JSON.");

        if (!OperatingSystem.IsWindows())
        {
            Write(new ElevatorResponse
            {
                Ok = true,
                Message = $"Non-Windows host: rollback of '{journal.Id}' acknowledged; no SCM mutations.",
                Elevated = IsElevated(),
                Skipped = journal.Entries.Count,
            });
            return 0;
        }

        return RollbackWindows(journal);
    }

    [SupportedOSPlatform("windows")]
    private static int RollbackWindows(ApplyJournal journal)
    {
        var services = PlatformAdapters.CreateServiceController();
        var applied = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var entry in journal.Entries)
        {
            try
            {
                services.SetStartType(entry.ServiceName, entry.PriorStartType, entry.PriorDelayedAutostart);
                applied++;
            }
            catch (Exception ex)
            {
                skipped++;
                errors.Add($"{entry.ServiceName}: {ex.Message}");
            }
        }

        Write(new ElevatorResponse
        {
            Ok = errors.Count == 0,
            Message = errors.Count == 0
                ? $"Rolled back {applied} service(s) from journal '{journal.Id}'."
                : $"Rollback partial: {applied} ok, {skipped} failed. " + string.Join("; ", errors),
            Elevated = IsElevated(),
            Applied = applied,
            Skipped = skipped,
        });
        return errors.Count == 0 ? 0 : 1;
    }

    private static bool IsElevated()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static int Fail(string message)
    {
        Write(new ElevatorResponse { Ok = false, Message = message });
        return 1;
    }

    private static void Write(ElevatorResponse response)
    {
        Console.WriteLine(JsonSerializer.Serialize(response, ElevatorJsonContext.Default.ElevatorResponse));
    }
}

internal sealed class ElevatorPlan
{
    public string Id { get; set; } = "";

    public List<ElevatorChange> Changes { get; set; } = [];
}

internal sealed class ElevatorChange
{
    public string ServiceName { get; set; } = "";

    public string TargetStartType { get; set; } = "";

    public bool DelayedAutostart { get; set; }
}

internal sealed class ElevatorResponse
{
    public bool Ok { get; set; }

    public string Message { get; set; } = "";

    public bool Elevated { get; set; }

    public string? Platform { get; set; }

    public int Applied { get; set; }

    public int Skipped { get; set; }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(ElevatorPlan))]
[JsonSerializable(typeof(ElevatorChange))]
[JsonSerializable(typeof(ElevatorResponse))]
[JsonSerializable(typeof(List<ElevatorChange>))]
[JsonSerializable(typeof(ApplyJournal))]
[JsonSerializable(typeof(JournalEntry))]
[JsonSerializable(typeof(List<JournalEntry>))]
[JsonSerializable(typeof(ServiceStartType))]
[JsonSerializable(typeof(JournalEntryStatus))]
internal partial class ElevatorJsonContext : JsonSerializerContext
{
}
