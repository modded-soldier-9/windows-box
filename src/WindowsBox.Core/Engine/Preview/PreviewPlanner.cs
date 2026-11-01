using WindowsBox.Core.Abstractions;
using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Core.Engine.Preview;

public sealed class PreviewPlanner
{
    private readonly IServiceController _services;
    private readonly ISystemProbe _probe;

    public PreviewPlanner(IServiceController services, ISystemProbe probe)
    {
        _services = services;
        _probe = probe;
    }

    public PreviewPlan Build(CatalogDocument catalog, OptimizationProfile profile)
    {
        var probe = _probe.Probe();
        var changes = new List<PreviewChange>();
        var skipped = new List<PreviewSkip>();

        foreach (var entry in catalog.Services)
        {
            var gateReason = EvaluateGates(entry, probe);
            if (gateReason is not null)
            {
                skipped.Add(new PreviewSkip
                {
                    ItemId = entry.Id,
                    ServiceName = entry.Name,
                    Reason = gateReason,
                });
                continue;
            }

            if (entry.TouchPolicy is TouchPolicy.Never or TouchPolicy.Leave)
            {
                skipped.Add(new PreviewSkip
                {
                    ItemId = entry.Id,
                    ServiceName = entry.Name,
                    Reason = $"touch_policy={entry.TouchPolicy}",
                });
                continue;
            }

            if (entry.TouchPolicy == TouchPolicy.PolicyOnly)
            {
                skipped.Add(new PreviewSkip
                {
                    ItemId = entry.Id,
                    ServiceName = entry.Name,
                    Reason = "Managed by Windows — use policy alternative, not service config.",
                });
                continue;
            }

            if (entry.Protected)
            {
                skipped.Add(new PreviewSkip
                {
                    ItemId = entry.Id,
                    ServiceName = entry.Name,
                    Reason = "Protected service (refuses sc config).",
                });
                continue;
            }

            if (!entry.Recommend.TryGetValue(profile, out var target))
            {
                skipped.Add(new PreviewSkip
                {
                    ItemId = entry.Id,
                    ServiceName = entry.Name,
                    Reason = $"No recommendation for profile {profile}.",
                });
                continue;
            }

            // Principle: Manual before Disabled — coerce accidental Disabled at risk>2.
            if (target == ServiceStartType.Disabled && entry.Risk > 2)
            {
                target = ServiceStartType.Manual;
            }

            var live = _services.GetConfig(entry.Name);
            if (live is null || !live.Exists)
            {
                skipped.Add(new PreviewSkip
                {
                    ItemId = entry.Id,
                    ServiceName = entry.Name,
                    Reason = entry.Optional ? "Optional service not present." : "Service not found.",
                });
                continue;
            }

            var current = Normalize(live);
            if (current == target)
            {
                skipped.Add(new PreviewSkip
                {
                    ItemId = entry.Id,
                    ServiceName = entry.Name,
                    Reason = "Already at target start type.",
                });
                continue;
            }

            if (WouldBreakDependency(entry, target, catalog))
            {
                skipped.Add(new PreviewSkip
                {
                    ItemId = entry.Id,
                    ServiceName = entry.Name,
                    Reason = "Dependency check blocked change (another Auto service depends on it).",
                });
                continue;
            }

            changes.Add(new PreviewChange
            {
                ItemId = entry.Id,
                ServiceName = entry.Name,
                Display = entry.Display,
                Current = current,
                Target = target,
                Risk = entry.Risk,
                Breaks = entry.Breaks,
                RequiresConfirm = entry.Risk >= 3 || target == ServiceStartType.Disabled,
                HonestyLabel = entry.HonestyLabel,
            });
        }

        return new PreviewPlan
        {
            Profile = profile,
            Changes = changes,
            Skipped = skipped,
            EstimatedIdleRamSavedMb = EstimateRam(changes),
        };
    }

    private static string? EvaluateGates(ServiceCatalogEntry entry, SystemProbeResult probe)
    {
        foreach (var role in entry.Gates.Not)
        {
            if (probe.HasRole(role))
            {
                return $"Gated: role '{role}' is present.";
            }
        }

        if (entry.Gates.HardwareAny.Count > 0)
        {
            var any = entry.Gates.HardwareAny.Any(probe.HasRole);
            if (!any)
            {
                return $"Gated: none of hardware [{string.Join(",", entry.Gates.HardwareAny)}] present.";
            }
        }

        if (entry.Gates.FeatureAny.Count > 0)
        {
            var any = entry.Gates.FeatureAny.Any(f => probe.EnabledFeatures.Contains(f));
            if (!any)
            {
                return $"Gated: none of features [{string.Join(",", entry.Gates.FeatureAny)}] enabled.";
            }
        }

        if (!BuildMatches(entry.Builds, probe.Build))
        {
            return $"Gated: build {probe.Build} does not satisfy {entry.Builds}.";
        }

        return null;
    }

    private static bool BuildMatches(string constraint, int build)
    {
        if (constraint.StartsWith(">=", StringComparison.Ordinal) &&
            int.TryParse(constraint[2..], out var min))
        {
            return build >= min;
        }

        return true;
    }

    private static ServiceStartType Normalize(ServiceConfig live)
    {
        if (live.StartType == ServiceStartType.Automatic && live.DelayedAutostart)
        {
            return ServiceStartType.AutomaticDelayed;
        }

        if (live.StartType == ServiceStartType.Manual && live.HasTriggers)
        {
            return ServiceStartType.ManualTrigger;
        }

        return live.StartType;
    }

    private bool WouldBreakDependency(
        ServiceCatalogEntry entry,
        ServiceStartType target,
        CatalogDocument catalog)
    {
        if (target is not (ServiceStartType.Manual or ServiceStartType.ManualTrigger or ServiceStartType.Disabled))
        {
            return false;
        }

        var live = _services.GetConfig(entry.Name);
        if (live is null)
        {
            return false;
        }

        foreach (var dependentName in live.DependentServices)
        {
            var depConfig = _services.GetConfig(dependentName);
            if (depConfig is null || !depConfig.Exists)
            {
                continue;
            }

            if (depConfig.StartType is ServiceStartType.Automatic or ServiceStartType.AutomaticDelayed)
            {
                // Allow if the dependent is itself being moved away from Auto in this catalog plan later —
                // M0: conservative block.
                var depEntry = catalog.FindByName(dependentName);
                if (depEntry is null || depEntry.TouchPolicy == TouchPolicy.Never)
                {
                    return true;
                }

                return true;
            }
        }

        return false;
    }

    private static int EstimateRam(IReadOnlyList<PreviewChange> changes)
    {
        // Provisional heuristic until ETW measurement lands: ~8 MB per Auto→Manual, ~12 MB Disabled.
        var total = 0;
        foreach (var change in changes)
        {
            if (change.Current is ServiceStartType.Automatic or ServiceStartType.AutomaticDelayed)
            {
                total += change.Target == ServiceStartType.Disabled ? 12 : 8;
            }
        }

        return total;
    }
}
