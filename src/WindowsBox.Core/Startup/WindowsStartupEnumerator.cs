using System.Runtime.Versioning;
using WindowsBox.Core.Abstractions;

namespace WindowsBox.Core.Startup;

/// <summary>
/// Live startup enumerator. Reads Run keys, startup folders, and (optionally)
/// services via injected adapters. Scheduled tasks / shell extensions / drivers
/// are listed as location stubs until deeper providers land — still covered in
/// the catalog model with risk ratings.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsStartupEnumerator : IStartupEnumerator
{
    private readonly IRegistry _registry;
    private readonly IServiceController _services;

    public WindowsStartupEnumerator(IRegistry registry, IServiceController services)
    {
        _registry = registry;
        _services = services;
    }

    public IReadOnlyList<StartupEntry> Enumerate(StartupEnumerateOptions? options = null)
    {
        options ??= new StartupEnumerateOptions();
        var list = new List<StartupEntry>();

        AddRunKey(list, RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            StartupLocationKind.RunKeyHkcu);
        AddRunKey(list, RegistryHive.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            StartupLocationKind.RunKeyHklm);
        AddRunKey(list, RegistryHive.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
            StartupLocationKind.RunOnceHkcu);
        AddRunKey(list, RegistryHive.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
            StartupLocationKind.RunOnceHklm);

        AddStartupFolder(list,
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            StartupLocationKind.StartupFolderUser);
        AddStartupFolder(list,
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
            StartupLocationKind.StartupFolderCommon);

        if (options.IncludeServices)
        {
            foreach (var name in _services.EnumerateServices())
            {
                var cfg = _services.GetConfig(name);
                if (cfg is null || !cfg.Exists)
                {
                    continue;
                }

                if (cfg.StartType is not Catalog.Models.ServiceStartType.Automatic
                    and not Catalog.Models.ServiceStartType.AutomaticDelayed)
                {
                    continue;
                }

                var microsoft = name.StartsWith("W", StringComparison.Ordinal) // weak heuristic
                                || string.Equals(cfg.DisplayName, name, StringComparison.OrdinalIgnoreCase);
                list.Add(new StartupEntry
                {
                    Id = "svc." + name.ToLowerInvariant(),
                    Name = cfg.DisplayName ?? name,
                    Location = StartupLocationKind.Service,
                    Command = name,
                    IsMicrosoft = microsoft,
                    Risk = StartupRiskClassifier.Classify(StartupLocationKind.Service, microsoft, name),
                    Enabled = true,
                });
            }
        }

        // Location coverage markers so the UI can show Autoruns-parity categories
        // even before deep providers (tasks/drivers/shell) are fully wired.
        if (options.IncludeScheduledTasks)
        {
            list.Add(new StartupEntry
            {
                Id = "task.coverage-marker",
                Name = "(Scheduled tasks — enumerator uses Task Scheduler API in a follow-up)",
                Location = StartupLocationKind.ScheduledTask,
                Command = "",
                IsMicrosoft = true,
                Enabled = false,
                Risk = 0,
                SignatureStatus = "Coverage placeholder",
            });
        }

        if (options.IncludeDrivers)
        {
            list.Add(new StartupEntry
            {
                Id = "drv.coverage-marker",
                Name = "(Boot/System drivers — enumerated via SCM type DRIVER in a follow-up)",
                Location = StartupLocationKind.Driver,
                Command = "",
                IsMicrosoft = true,
                Enabled = false,
                Risk = 0,
                SignatureStatus = "Coverage placeholder",
            });
        }

        if (options.IncludeShellExtensions)
        {
            list.Add(new StartupEntry
            {
                Id = "shell.coverage-marker",
                Name = "(Shell extensions — Approved\\{} keys in a follow-up)",
                Location = StartupLocationKind.ShellExtension,
                Command = "",
                IsMicrosoft = true,
                Enabled = false,
                Risk = 0,
                SignatureStatus = "Coverage placeholder",
            });
        }

        IEnumerable<StartupEntry> q = list;
        if (options.HideMicrosoft)
        {
            q = q.Where(e => !e.IsMicrosoft);
        }

        return q.OrderBy(e => e.Location).ThenBy(e => e.Name).ToList();
    }

    private void AddRunKey(
        List<StartupEntry> list,
        RegistryHive hive,
        string subKey,
        StartupLocationKind kind)
    {
        // ExportKey / GetValue are per-value; without EnumerateValues on IRegistry we
        // only surface well-known names when present. Full value enumeration lands with
        // an IRegistry.EnumerateValues addition — for now read common Microsoft entries.
        foreach (var name in new[] { "SecurityHealth", "OneDrive", "WindowsDefender", "ctfmon" })
        {
            var value = _registry.GetValue(hive, subKey, name);
            if (value is null)
            {
                continue;
            }

            var command = Convert.ToString(value) ?? string.Empty;
            var microsoft = command.Contains("Windows", StringComparison.OrdinalIgnoreCase)
                            || command.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);
            list.Add(new StartupEntry
            {
                Id = $"{kind}.{name}".ToLowerInvariant(),
                Name = name,
                Location = kind,
                Command = command,
                IsMicrosoft = microsoft,
                Risk = StartupRiskClassifier.Classify(kind, microsoft, command),
                Enabled = true,
            });
        }
    }

    private static void AddStartupFolder(List<StartupEntry> list, string folder, StartupLocationKind kind)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(folder))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            list.Add(new StartupEntry
            {
                Id = $"{kind}.{name}".ToLowerInvariant().Replace(' ', '-'),
                Name = name,
                Location = kind,
                Command = file,
                IsMicrosoft = false,
                Risk = StartupRiskClassifier.Classify(kind, false, file),
                Enabled = true,
            });
        }
    }
}
