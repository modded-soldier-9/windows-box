using WindowsBox.Core.Catalog;
using WindowsBox.Core.Catalog.Models;
using WindowsBox.Core.Engine;
using WindowsBox.Core.Engine.Journal;
using WindowsBox.Core.Engine.Restore;
using WindowsBox.Core.Engine.Undo;
using WindowsBox.Core.Engine.Verify;
using WindowsBox.Core.Network;
using WindowsBox.Core.Platform;

return await WboxApp.RunAsync(args);

internal static class WboxApp
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintHelp();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        try
        {
            return command switch
            {
                "catalog" => RunCatalog(args.Skip(1).ToArray()),
                "preview" => RunPreview(args.Skip(1).ToArray()),
                "version" => RunVersion(),
                "apply" => await RunApplyAsync(args.Skip(1).ToArray(), dryRun: false),
                "dry-run" => await RunApplyAsync(args.Skip(1).ToArray(), dryRun: true),
                "rollback" => await RunRollbackAsync(args.Skip(1).ToArray()),
                "verify" => await RunVerifyAsync(args.Skip(1).ToArray()),
                "drift" => await RunDriftAsync(args.Skip(1).ToArray()),
                "journals" => await RunJournalsAsync(args.Skip(1).ToArray()),
                "netcheck" => RunNetCheck(args.Skip(1).ToArray()),
                _ => Unknown(command),
            };
        }
        catch (CatalogLoadException ex)
        {
            Console.Error.WriteLine(ex.Message);
            foreach (var error in ex.Errors)
            {
                Console.Error.WriteLine($"  - {error}");
            }

            return 2;
        }
        catch (PlatformNotSupportedException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine("Live SCM/registry adapters require Windows. Use fakes in unit tests.");
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 1;
    }

    private static int RunVersion()
    {
        Console.WriteLine("wbox 0.1.0-m1-engine");
        Console.WriteLine("Windows Box — .NET 10 LTS — no telemetry, no background service.");
        return 0;
    }

    private static int RunCatalog(string[] args)
    {
        var catalogDir = GetOption(args, "--catalog") ?? DefaultCatalogDir();
        var action = args.FirstOrDefault(a => !a.StartsWith('-')) ?? "validate";

        return action switch
        {
            "validate" => ValidateCatalog(catalogDir),
            "list" => ListCatalog(catalogDir),
            _ => Unknown("catalog " + action),
        };
    }

    private static int ValidateCatalog(string catalogDir)
    {
        var catalog = new ServiceCatalogLoader().LoadDirectory(catalogDir);
        Console.WriteLine($"OK: {catalog.Services.Count} services validated from {catalogDir}");
        return 0;
    }

    private static int ListCatalog(string catalogDir)
    {
        var catalog = new ServiceCatalogLoader().LoadDirectory(catalogDir);
        foreach (var s in catalog.Services.OrderBy(s => s.Id))
        {
            Console.WriteLine($"{s.Id,-32} risk={s.Risk} touch={s.TouchPolicy,-11} {s.Name}");
        }

        return 0;
    }

    private static int RunPreview(string[] args)
    {
        var profile = ParseProfile(GetOption(args, "--profile") ?? "balanced");
        var catalog = new ServiceCatalogLoader().LoadDirectory(GetOption(args, "--catalog") ?? DefaultCatalogDir());
        var pipeline = CreatePipeline(GetOption(args, "--journal-dir") ?? DefaultJournalDir());
        var plan = pipeline.DetectAndPreview(catalog, profile);

        Console.WriteLine($"Profile: {plan.Profile}");
        Console.WriteLine($"Diff: {plan.DiffSummary}");
        Console.WriteLine(plan.HonestyNote);
        Console.WriteLine();
        foreach (var change in plan.Changes)
        {
            Console.WriteLine($"  {change.DiffLine}");
            if (change.Breaks.Count > 0)
            {
                Console.WriteLine($"    breaks: {string.Join("; ", change.Breaks)}");
            }
        }

        if (plan.Skipped.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Skipped ({plan.Skipped.Count}):");
            foreach (var skip in plan.Skipped.Take(20))
            {
                Console.WriteLine($"  {skip.ServiceName}: {skip.Reason}");
            }

            if (plan.Skipped.Count > 20)
            {
                Console.WriteLine($"  … {plan.Skipped.Count - 20} more");
            }
        }

        return 0;
    }

    private static async Task<int> RunApplyAsync(string[] args, bool dryRun)
    {
        var profile = ParseProfile(GetOption(args, "--profile") ?? "balanced");
        var confirm = HasFlag(args, "--confirm");
        var catalog = new ServiceCatalogLoader().LoadDirectory(GetOption(args, "--catalog") ?? DefaultCatalogDir());
        var journalDir = GetOption(args, "--journal-dir") ?? DefaultJournalDir();
        var pipeline = CreatePipeline(journalDir);

        if (!OperatingSystem.IsWindows() && !HasFlag(args, "--allow-non-windows"))
        {
            Console.Error.WriteLine(
                "Refusing to apply on a non-Windows host. Unit tests cover the pipeline with fakes.");
            return 3;
        }

        var result = await pipeline.ApplyAsync(
            catalog,
            profile,
            dryRun: dryRun,
            confirmHighRisk: confirm);

        Console.WriteLine(dryRun ? "Dry-run journal saved (no mutations)." : "Apply complete.");
        Console.WriteLine($"Journal: {result.Journal.Id}");
        Console.WriteLine($"Health: {(result.Health.Passed ? "passed" : "FAILED")}");
        Console.WriteLine($"Verify: {(result.Verify.Passed ? "passed" : "FAILED")}");
        if (result.Restore is not null)
        {
            Console.WriteLine($"Offline restore kit: {result.Restore.Directory}");
        }

        return result.Health.Passed && (dryRun || result.Verify.Passed) ? 0 : 4;
    }

    private static async Task<int> RunRollbackAsync(string[] args)
    {
        var id = args.FirstOrDefault(a => !a.StartsWith('-'));
        if (string.IsNullOrWhiteSpace(id))
        {
            Console.Error.WriteLine("usage: wbox rollback <journal-id>");
            return 1;
        }

        if (!OperatingSystem.IsWindows() && !HasFlag(args, "--allow-non-windows"))
        {
            Console.Error.WriteLine("Refusing to rollback on a non-Windows host.");
            return 3;
        }

        var pipeline = CreatePipeline(GetOption(args, "--journal-dir") ?? DefaultJournalDir());
        var journal = await pipeline.RollbackAsync(id);
        Console.WriteLine($"Rolled back journal {journal.Id}");
        foreach (var entry in journal.Entries)
        {
            Console.WriteLine($"  {entry.ServiceName}: {entry.Status}{(entry.Error is null ? "" : " — " + entry.Error)}");
        }

        return 0;
    }

    private static async Task<int> RunVerifyAsync(string[] args)
    {
        var quiet = HasFlag(args, "--quiet");
        var journalDir = GetOption(args, "--journal-dir") ?? DefaultJournalDir();
        var store = new FileJournalStore(journalDir);
        var services = PlatformAdapters.CreateServiceController();

        var id = args.FirstOrDefault(a => !a.StartsWith('-') && a != "--quiet");
        if (id is not null)
        {
            var pipeline = CreatePipeline(journalDir);
            var journal = await store.LoadAsync(id)
                          ?? throw new InvalidOperationException($"Journal not found: {id}");
            var verify = pipeline.VerifyJournal(journal);
            if (!quiet)
            {
                Console.WriteLine($"Verify {verify.JournalId}: {(verify.Passed ? "passed" : "FAILED")}");
                foreach (var item in verify.Items)
                {
                    Console.WriteLine($"  {(item.Passed ? "OK" : "DRIFT")} {item.ServiceName}: {item.Detail}");
                }
            }

            return verify.Passed ? 0 : 5;
        }

        // App-open path: one-shot stuck + drift check. Notify only on drift when --quiet.
        var report = await StartupVerifier.Create(store, services).RunAsync(quiet);
        if (!quiet)
        {
            Console.WriteLine(report.Summary);
            foreach (var item in report.Drift.Drifted.Concat(report.Drift.Missing))
            {
                Console.WriteLine(
                    $"  {item.ServiceName}: expected {item.Expected}, actual {item.Actual?.ToString() ?? "missing"} — {item.Reason}");
            }
        }
        else if (!report.Passed)
        {
            Console.WriteLine($"drift:{report.Drift.Drifted.Count + report.Drift.Missing.Count + report.StuckFailures}");
        }

        return report.Passed ? 0 : 5;
    }

    private static async Task<int> RunDriftAsync(string[] args) =>
        await RunVerifyAsync(args);

    private static async Task<int> RunJournalsAsync(string[] args)
    {
        var store = new FileJournalStore(GetOption(args, "--journal-dir") ?? DefaultJournalDir());
        var list = await store.ListAsync();
        foreach (var journal in list)
        {
            Console.WriteLine($"{journal.Id}  {journal.CreatedAt:u}  {journal.Profile}  entries={journal.Entries.Count}");
        }

        return 0;
    }

    private static ApplyPipeline CreatePipeline(string journalDir)
    {
        var parent = Path.GetDirectoryName(journalDir.TrimEnd(Path.DirectorySeparatorChar)) ?? journalDir;
        var backupDir = Path.Combine(parent, "backups");
        var undoDir = Path.Combine(parent, "undo");
        var services = PlatformAdapters.CreateServiceController();
        var registry = PlatformAdapters.CreateRegistry();
        return new ApplyPipeline(
            services,
            registry,
            PlatformAdapters.CreateSystemProbe(),
            new FileJournalStore(journalDir),
            restoreKit: new OfflineRestoreKit(backupDir),
            undo: new FileUndoLayer(undoDir, services, registry));
    }

    private static OptimizationProfile ParseProfile(string name)
    {
        if (Enum.TryParse<OptimizationProfile>(name, ignoreCase: true, out var profile))
        {
            return profile;
        }

        throw new ArgumentException(
            $"Unknown profile '{name}'. Expected one of: {string.Join(", ", Enum.GetNames<OptimizationProfile>())}");
    }

    private static string DefaultCatalogDir()
    {
        var env = Environment.GetEnvironmentVariable("WBOX_CATALOG");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "catalog", "services");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "catalog", "services"));
    }

    private static string DefaultJournalDir()
    {
        if (OperatingSystem.IsWindows())
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(programData, "WindowsBox", "journals");
        }

        return Path.Combine(Path.GetTempPath(), "WindowsBox", "journals");
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static bool HasFlag(string[] args, string name) =>
        args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));


    private static int RunNetCheck(string[] args)
    {
        var module = new NetworkPrivacyModule();
        var report = module.Run(new NetworkPrivacyOptions
        {
            IncludeLoopback = HasFlag(args, "--loopback"),
            ResolveRemoteHostNames = HasFlag(args, "--resolve"),
        });
        Console.WriteLine(module.FormatPlainText(report));
        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            wbox — Windows Box CLI

            Usage:
              wbox catalog validate [--catalog <dir>]
              wbox catalog list [--catalog <dir>]
              wbox preview --profile <name> [--catalog <dir>]
              wbox dry-run --profile <name> [--confirm]
              wbox apply --profile <name> [--confirm]
              wbox rollback <journal-id>
              wbox verify [--quiet] [<journal-id>]
              wbox drift [--quiet]
              wbox netcheck [--resolve] [--loopback]
              wbox journals
              wbox version

            Principles:
              - No background service, no phone-home, no tweaks on install
              - Prefer Manual over Disabled
              - Every apply snapshots real prior values for undo
              - Never run apply/rollback against CI Linux hosts (fakes only)
            """);
    }
}
