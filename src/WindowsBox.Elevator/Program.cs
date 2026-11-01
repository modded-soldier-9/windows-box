using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Short-lived elevated helper. Launched per apply batch via UAC, exits after work.
/// M0: protocol + dry validation only; real SCM calls land behind Windows-only adapters in M1.
/// </summary>
return ElevatorHost.Run(args);

internal static class ElevatorHost
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

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
                "--apply-plan" => ApplyPlan(RequirePath(args, 1)),
                "--rollback-journal" => Rollback(RequirePath(args, 1)),
                _ => Fail($"Unknown argument: {args[0]}"),
            };
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
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
        var plan = JsonSerializer.Deserialize<ElevatorPlan>(stream, JsonOptions)
                   ?? throw new InvalidOperationException("Invalid plan JSON.");

        // M0 stub: acknowledge without mutating the host.
        Write(new ElevatorResponse
        {
            Ok = true,
            Message = $"Accepted plan '{plan.Id}' with {plan.Changes.Count} change(s); SCM apply stubbed until M1 Windows adapter.",
            Elevated = IsElevated(),
            Applied = 0,
            Skipped = plan.Changes.Count,
        });
        return 0;
    }

    private static int Rollback(string path)
    {
        if (!File.Exists(path))
        {
            return Fail($"Journal not found: {path}");
        }

        Write(new ElevatorResponse
        {
            Ok = true,
            Message = "Rollback acknowledged (SCM restore stubbed until M1).",
            Elevated = IsElevated(),
        });
        return 0;
    }

    private static bool IsElevated()
    {
        // Real WindowsIdentity check arrives with the Windows SCM adapter (M1).
        // Keeping this TFM net9.0 so Linux CI can compile the helper host.
        return false;
    }

    private static int Fail(string message)
    {
        Write(new ElevatorResponse { Ok = false, Message = message });
        return 1;
    }

    private static void Write(ElevatorResponse response)
    {
        Console.WriteLine(JsonSerializer.Serialize(response, JsonOptions));
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
