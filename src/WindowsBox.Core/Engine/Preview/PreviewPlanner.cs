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

            if (entry.PerUser)
            {
                skipped.Add(new PreviewSkip
                {
                    ItemId = entry.Id,
                    ServiceName = entry.Name,
                    Reason = "Per-user template (instance resolution lands in M1).",
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

            var gain = EstimateGainMb(current, target);
            changes.Add(new PreviewChange
            {
                ItemId = entry.Id,
                ServiceName = entry.Name,
                Display = entry.Display,
                Current = current,
                Target = target,
                Risk = entry.Risk,
                ExpectedGainMb = gain,
                Breaks = entry.Breaks,
                RequiresConfirm = entry.Risk >= 3 || target == ServiceStartType.Disabled,
                HonestyLabel = entry.HonestyLabel,
            });
        }

        var totalGain = changes.Sum(c => c.ExpectedGainMb);
        return new PreviewPlan
        {
            Profile = profile,
            Changes = changes,
            Skipped = skipped,
            EstimatedIdleRamSavedMb = totalGain,
            DiffSummary = BuildDiffSummary(changes, totalGain),
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
        // M0: always block when any Automatic dependent exists.
        // M1 may allow when that dependent is also moving away from Auto in the same plan.
        _ = catalog;

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
                return true;
            }
        }

        return false;
    }

    private static int EstimateGainMb(ServiceStartType current, ServiceStartType target)
    {
        // Provisional heuristic until ETW measurement lands.
        if (current is not (ServiceStartType.Automatic or ServiceStartType.AutomaticDelayed))
        {
            return 0;
        }

        return target == ServiceStartType.Disabled ? 12 : 8;
    }

    private static string BuildDiffSummary(IReadOnlyList<PreviewChange> changes, int totalGainMb)
    {
        if (changes.Count == 0)
        {
            return "No changes.";
        }

        var toManual = changes.Count(c =>
            c.Target is ServiceStartType.Manual or ServiceStartType.ManualTrigger);
        var toDisabled = changes.Count(c => c.Target == ServiceStartType.Disabled);
        var other = changes.Count - toManual - toDisabled;

        var parts = new List<string> { $"{changes.Count} service(s)" };
        if (toManual > 0)
        {
            parts.Add($"{toManual} → Manual");
        }

        if (toDisabled > 0)
        {
            parts.Add($"{toDisabled} → Disabled");
        }

        if (other > 0)
        {
            parts.Add($"{other} other");
        }

        parts.Add($"est. −{totalGainMb} MB idle");
        return string.Join("; ", parts);
    }
}
