using System.Text.Json;
using System.Text.Json.Serialization;
using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Core.Catalog;

/// <summary>
/// Loads the 290-service research inventory (<c>docs/catalogs/services-full.json</c>)
/// into engine catalog entries. Unverified defaults keep honesty_label=unproven.
/// Curated YAML in <c>catalog/services</c> remains the apply-ready source of truth;
/// this inventory expands coverage for browse/search/advisory.
/// </summary>
public sealed class FullServicesInventoryLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public CatalogDocument LoadFile(string path)
    {
        using var stream = File.OpenRead(path);
        var inventory = JsonSerializer.Deserialize<FullServicesInventory>(stream, JsonOptions)
                        ?? throw new CatalogLoadException($"Failed to parse inventory: {path}");

        if (inventory.Services.Count == 0)
        {
            throw new CatalogLoadException("Inventory contains zero services.");
        }

        var entries = inventory.Services.Select(Map).ToList();
        return new CatalogDocument { Services = entries };
    }

    private static ServiceCatalogEntry Map(FullServiceRow row)
    {
        var id = "svc." + Sanitize(row.Name);
        var defaults = new Dictionary<string, ServiceStartType>(StringComparer.OrdinalIgnoreCase);
        foreach (var (edition, value) in row.Defaults)
        {
            defaults[edition] = ParseStart(value);
        }

        if (!defaults.ContainsKey("pro"))
        {
            defaults["pro"] = ParseStart(row.StartCode ?? "Manual");
        }

        var touch = row.Protected
            ? TouchPolicy.Never
            : row.Risk >= 4
                ? TouchPolicy.Leave
                : TouchPolicy.Allow;

        // Protected WU/security-ish names route to policy_only when known.
        if (IsPolicyOnly(row.Name))
        {
            touch = TouchPolicy.PolicyOnly;
        }

        var recommend = BuildRecommend(row, ParseStart(row.StartCode ?? "Manual"));

        return new ServiceCatalogEntry
        {
            SchemaVersion = 1,
            Id = id,
            Name = row.Name,
            Display = string.IsNullOrWhiteSpace(row.Display) ? row.Name : row.Display,
            Category = InferCategory(row),
            Description = row.Description ?? row.Name,
            Defaults = defaults,
            Builds = ">=22621",
            Risk = Math.Clamp(row.Risk, 0, 4),
            Protected = row.Protected,
            PerUser = row.PerUser,
            Optional = string.Equals(row.Presence, "optional", StringComparison.OrdinalIgnoreCase),
            TouchPolicy = touch,
            Recommend = recommend,
            DependsOn = row.DependsOn ?? [],
            Notes = row.Notes,
            HonestyLabel = row.Verified ? "measured" : "unproven",
            Sources = ["https://github.com/modded-soldier-9/windows-box/blob/main/docs/catalogs/services-full.json"],
            Alternatives = touch == TouchPolicy.PolicyOnly
                ? [new PolicyAlternative
                {
                    Policy = $"policy alternative for protected service {row.Name}",
                    Description = "Do not sc config this service; use the documented policy route instead.",
                }]
                : [],
        };
    }

    private static Dictionary<OptimizationProfile, ServiceStartType> BuildRecommend(
        FullServiceRow row,
        ServiceStartType currentDefault)
    {
        var map = new Dictionary<OptimizationProfile, ServiceStartType>();
        if (row.Risk == 0 || row.Protected)
        {
            return map;
        }

        // Manual before Disabled — only risk ≤2 may recommend Disabled.
        var safeTarget = row.Risk <= 2 && LooksTelemetry(row)
            ? ServiceStartType.Disabled
            : ServiceStartType.Manual;

        if (safeTarget == ServiceStartType.Disabled && row.Risk > 2)
        {
            safeTarget = ServiceStartType.Manual;
        }

        // Don't recommend a change when already Manual/Disabled by default.
        if (currentDefault is ServiceStartType.Manual or ServiceStartType.ManualTrigger or ServiceStartType.Disabled)
        {
            if (LooksTelemetry(row) && currentDefault != ServiceStartType.Disabled && row.Risk <= 2)
            {
                map[OptimizationProfile.Privacy] = ServiceStartType.Disabled;
                map[OptimizationProfile.Minimal] = ServiceStartType.Disabled;
            }

            return map;
        }

        map[OptimizationProfile.Balanced] = ServiceStartType.Manual;
        map[OptimizationProfile.Privacy] = safeTarget;
        map[OptimizationProfile.Gamer] = safeTarget;
        map[OptimizationProfile.Minimal] = safeTarget;
        map[OptimizationProfile.Laptop] = ServiceStartType.Manual;
        map[OptimizationProfile.Safe] = row.Risk <= 1 ? ServiceStartType.Manual : currentDefault;
        return map;
    }

    private static bool LooksTelemetry(FullServiceRow row)
    {
        var blob = $"{row.Name} {row.Display} {row.Description} {row.Notes}".ToLowerInvariant();
        return blob.Contains("telemetry", StringComparison.Ordinal)
               || blob.Contains("diagnostic", StringComparison.Ordinal)
               || blob.Contains("diagtrack", StringComparison.Ordinal)
               || blob.Contains("customer experience", StringComparison.Ordinal);
    }

    private static bool IsPolicyOnly(string name) =>
        name is "WaaSMedicSvc" or "DoSvc" or "UsoSvc" or "wuauserv" or "WinDefend" or "WdNisSvc" or "mpssvc";

    private static string InferCategory(FullServiceRow row)
    {
        var blob = $"{row.Name} {row.Display} {row.Description}".ToLowerInvariant();
        if (LooksTelemetry(row)) return "telemetry";
        if (blob.Contains("xbox", StringComparison.Ordinal) || blob.Contains("game", StringComparison.Ordinal)) return "gaming";
        if (blob.Contains("print", StringComparison.Ordinal) || blob.Contains("bluetooth", StringComparison.Ordinal) || blob.Contains("camera", StringComparison.Ordinal)) return "hardware";
        if (blob.Contains("update", StringComparison.Ordinal)) return "updates";
        if (blob.Contains("defend", StringComparison.Ordinal) || blob.Contains("security", StringComparison.Ordinal)) return "security";
        if (blob.Contains("hyper-v", StringComparison.Ordinal) || blob.Contains("vm", StringComparison.Ordinal) || blob.Contains("wsl", StringComparison.Ordinal)) return "virtualization";
        if (blob.Contains("network", StringComparison.Ordinal) || blob.Contains("wlan", StringComparison.Ordinal) || blob.Contains("wwan", StringComparison.Ordinal)) return "network";
        if (blob.Contains("privacy", StringComparison.Ordinal)) return "privacy";
        return "performance";
    }

    private static ServiceStartType ParseStart(string value) => value.Trim() switch
    {
        "Automatic" or "Auto" => ServiceStartType.Automatic,
        "AutomaticDelayed" or "Automatic (Delayed Start)" or "DelayedAuto" => ServiceStartType.AutomaticDelayed,
        "Manual" or "Demand" => ServiceStartType.Manual,
        "ManualTrigger" or "AutomaticTrigger" => ServiceStartType.ManualTrigger,
        "Disabled" => ServiceStartType.Disabled,
        _ => ServiceStartType.Manual,
    };

    private static string Sanitize(string name)
    {
        var chars = name.ToLowerInvariant().Select(c =>
            char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '-').ToArray();
        return new string(chars).Trim('-');
    }

    private sealed class FullServicesInventory
    {
        public List<FullServiceRow> Services { get; set; } = [];
    }

    private sealed class FullServiceRow
    {
        public string Name { get; set; } = "";

        [JsonPropertyName("per_user")]
        public bool PerUser { get; set; }

        public string? Display { get; set; }

        public Dictionary<string, string> Defaults { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("start_code")]
        public string? StartCode { get; set; }

        public string? Description { get; set; }

        [JsonPropertyName("depends_on")]
        public List<string>? DependsOn { get; set; }

        public int Risk { get; set; }

        public string? Presence { get; set; }

        public string? Notes { get; set; }

        public bool Protected { get; set; }

        public bool Verified { get; set; }
    }
}
