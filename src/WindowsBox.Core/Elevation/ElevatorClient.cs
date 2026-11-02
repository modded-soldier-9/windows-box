using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine.Journal;

namespace WindowsBox.Core.Elevation;

/// <summary>
/// Launches <c>WindowsBox.Elevator</c> elevated for a single batch, waits for exit,
/// and never leaves a resident process. Unelevated UI/CLI call this only for apply/rollback.
/// </summary>
public sealed class ElevatorClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = true,
    };

    private readonly string _elevatorPath;
    private readonly string _workDirectory;

    public ElevatorClient(string elevatorPath, string? workDirectory = null)
    {
        _elevatorPath = elevatorPath;
        _workDirectory = workDirectory ?? Path.Combine(Path.GetTempPath(), "WindowsBox", "elevator");
        Directory.CreateDirectory(_workDirectory);
    }

    public async Task<ElevatorClientResult> ApplyJournalAsync(
        ApplyJournal journal,
        bool requireElevation = true,
        CancellationToken cancellationToken = default)
    {
        var plan = new ElevatorPlanDto
        {
            Id = journal.Id,
            Changes = journal.Entries
                .Where(e => e.Status is JournalEntryStatus.Pending or JournalEntryStatus.Applied)
                .Select(e => new ElevatorChangeDto
                {
                    ServiceName = e.ServiceName,
                    TargetStartType = e.TargetStartType.ToString(),
                    DelayedAutostart = e.TargetDelayedAutostart,
                })
                .ToList(),
        };

        var planPath = Path.Combine(_workDirectory, $"{journal.Id}-plan.json");
        await File.WriteAllTextAsync(planPath, JsonSerializer.Serialize(plan, JsonOptions), cancellationToken);
        return await RunAsync(["--apply-plan", planPath], requireElevation, cancellationToken);
    }

    public async Task<ElevatorClientResult> RollbackJournalAsync(
        ApplyJournal journal,
        bool requireElevation = true,
        CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_workDirectory, $"{journal.Id}-rollback.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(journal, JsonOptions), cancellationToken);
        return await RunAsync(["--rollback-journal", path], requireElevation, cancellationToken);
    }

    public Task<ElevatorClientResult> PingAsync(CancellationToken cancellationToken = default) =>
        RunAsync(["--ping"], requireElevation: false, cancellationToken);

    private async Task<ElevatorClientResult> RunAsync(
        string[] args,
        bool requireElevation,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_elevatorPath) && !OperatingSystem.IsWindows())
        {
            // Linux CI: elevator host still compiles; client returns a dry acknowledgment.
            return new ElevatorClientResult
            {
                Ok = true,
                Message = "Elevator skipped on non-Windows (host process not launched).",
                Elevated = false,
                ExitCode = 0,
            };
        }

        var psi = new ProcessStartInfo
        {
            FileName = _elevatorPath,
            UseShellExecute = requireElevation && OperatingSystem.IsWindows(),
            RedirectStandardOutput = !(requireElevation && OperatingSystem.IsWindows()),
            RedirectStandardError = !(requireElevation && OperatingSystem.IsWindows()),
            CreateNoWindow = true,
        };

        if (requireElevation && OperatingSystem.IsWindows())
        {
            // UAC elevation — shell execute with runas; stdout capture unavailable in that mode.
            psi.Verb = "runas";
            foreach (var arg in args)
            {
                psi.ArgumentList.Add(arg);
            }
        }
        else
        {
            psi.FileName = "dotnet";
            psi.ArgumentList.Add(_elevatorPath);
            foreach (var arg in args)
            {
                psi.ArgumentList.Add(arg);
            }
        }

        using var process = new Process { StartInfo = psi };
        try
        {
            if (!process.Start())
            {
                return new ElevatorClientResult { Ok = false, Message = "Failed to start Elevator.", ExitCode = -1 };
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return new ElevatorClientResult { Ok = false, Message = "UAC elevation cancelled by user.", ExitCode = 1223 };
        }

        await process.WaitForExitAsync(cancellationToken);
        var stdout = psi.RedirectStandardOutput ? await process.StandardOutput.ReadToEndAsync(cancellationToken) : "";

        ElevatorResponseDto? parsed = null;
        if (!string.IsNullOrWhiteSpace(stdout))
        {
            try
            {
                parsed = JsonSerializer.Deserialize<ElevatorResponseDto>(stdout.Trim(), JsonOptions);
            }
            catch (JsonException)
            {
                // keep raw
            }
        }

        return new ElevatorClientResult
        {
            Ok = process.ExitCode == 0 && (parsed?.Ok ?? process.ExitCode == 0),
            Message = parsed?.Message ?? stdout,
            Elevated = parsed?.Elevated ?? requireElevation,
            Applied = parsed?.Applied ?? 0,
            Skipped = parsed?.Skipped ?? 0,
            ExitCode = process.ExitCode,
            RawStdout = stdout,
        };
    }
}

public sealed class ElevatorClientResult
{
    public required bool Ok { get; init; }

    public string Message { get; init; } = "";

    public bool Elevated { get; init; }

    public int Applied { get; init; }

    public int Skipped { get; init; }

    public int ExitCode { get; init; }

    public string? RawStdout { get; init; }
}

public sealed class ElevatorPlanDto
{
    public string Id { get; set; } = "";

    public List<ElevatorChangeDto> Changes { get; set; } = [];
}

public sealed class ElevatorChangeDto
{
    public string ServiceName { get; set; } = "";

    public string TargetStartType { get; set; } = "";

    public bool DelayedAutostart { get; set; }
}

public sealed class ElevatorResponseDto
{
    public bool Ok { get; set; }

    public string Message { get; set; } = "";

    public bool Elevated { get; set; }

    public string? Platform { get; set; }

    public int Applied { get; set; }

    public int Skipped { get; set; }
}
