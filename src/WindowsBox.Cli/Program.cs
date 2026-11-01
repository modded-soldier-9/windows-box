using WindowsBox.Core.Catalog;
using WindowsBox.Core.Engine.Journal;

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
                "preview" => RunPreview(),
                "version" => RunVersion(),
                "apply" => await RunApplyAsync(dryRun: false),
                "dry-run" => await RunApplyAsync(dryRun: true),
                "rollback" => await RunRollbackAsync(args.Skip(1).ToArray()),
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
        Console.WriteLine("wbox 0.0.1-m0");
        Console.WriteLine("Windows Box — no telemetry, no background service.");
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

    private static int RunPreview()
    {
        Console.WriteLine(
            "preview requires a live Windows service controller (M1). " +
            "On this host, use `wbox catalog validate` or unit tests with fakes.");
        return 0;
    }

    private static Task<int> RunApplyAsync(bool dryRun)
    {
        Console.WriteLine(
            dryRun
                ? "dry-run: elevated apply path is stubbed in M0; use unit tests for pipeline coverage."
                : "apply: launches WindowsBox.Elevator on Windows (stubbed in M0 non-Windows hosts).");
        return Task.FromResult(0);
    }

    private static async Task<int> RunRollbackAsync(string[] args)
    {
        var id = args.FirstOrDefault(a => !a.StartsWith('-'));
        if (string.IsNullOrWhiteSpace(id))
        {
            Console.Error.WriteLine("usage: wbox rollback <journal-id>");
            return 1;
        }

        var root = GetOption(args, "--journal-dir") ?? DefaultJournalDir();
        var store = new FileJournalStore(root);
        var journal = await store.LoadAsync(id);
        if (journal is null)
        {
            Console.Error.WriteLine($"Journal not found: {id}");
            return 1;
        }

        Console.WriteLine(
            $"Loaded journal {journal.Id} with {journal.Entries.Count} entries (rollback via elevator in M1).");
        return 0;
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

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            wbox — Windows Box CLI

            Usage:
              wbox catalog validate [--catalog <dir>]
              wbox catalog list [--catalog <dir>]
              wbox preview --profile <name>
              wbox dry-run --profile <name>
              wbox apply --profile <name>
              wbox rollback <journal-id>
              wbox version

            Principles:
              - No background service, no phone-home, no tweaks on install
              - Prefer Manual over Disabled
              - Every apply snapshots real prior values for undo
            """);
    }
}
