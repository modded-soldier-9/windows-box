using System.Text.RegularExpressions;
using WindowsBox.Core.Catalog.Models;

namespace WindowsBox.Core.Catalog;

public sealed partial class ServiceCatalogValidator
{
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "telemetry", "privacy", "gaming", "hardware", "performance",
        "network", "updates", "security", "virtualization",
    };

    private static readonly HashSet<string> AllowedHonesty = new(StringComparer.OrdinalIgnoreCase)
    {
        "unproven", "measured", "community",
    };

    public CatalogValidationResult Validate(ServiceCatalogEntry entry)
    {
        var errors = new List<string>();

        if (entry.SchemaVersion != 1)
        {
            errors.Add($"Unsupported schema_version {entry.SchemaVersion}.");
        }

        if (string.IsNullOrWhiteSpace(entry.Id) || !IdRegex().IsMatch(entry.Id))
        {
            errors.Add($"Invalid id '{entry.Id}' (expected svc.name).");
        }

        if (!string.Equals(entry.Area, "services", StringComparison.Ordinal))
        {
            errors.Add($"area must be 'services', got '{entry.Area}'.");
        }

        if (string.IsNullOrWhiteSpace(entry.Name))
        {
            errors.Add("name is required.");
        }

        if (string.IsNullOrWhiteSpace(entry.Display))
        {
            errors.Add("display is required.");
        }

        if (string.IsNullOrWhiteSpace(entry.Description))
        {
            errors.Add("description is required.");
        }

        if (!AllowedCategories.Contains(entry.Category))
        {
            errors.Add($"Unknown category '{entry.Category}'.");
        }

        if (entry.Risk is < 0 or > 4)
        {
            errors.Add($"risk must be 0–4, got {entry.Risk}.");
        }

        if (!entry.Defaults.ContainsKey("pro"))
        {
            errors.Add("defaults.pro is required.");
        }

        if (string.IsNullOrWhiteSpace(entry.Builds) || !BuildsRegex().IsMatch(entry.Builds))
        {
            errors.Add($"builds must look like '>=22621', got '{entry.Builds}'.");
        }

        if (entry.Sources.Count == 0)
        {
            errors.Add("At least one source is required.");
        }

        foreach (var source in entry.Sources)
        {
            if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                errors.Add($"Invalid source URI: {source}");
            }
        }

        if (entry.HonestyLabel is not null && !AllowedHonesty.Contains(entry.HonestyLabel))
        {
            errors.Add($"Unknown honesty_label '{entry.HonestyLabel}'.");
        }

        // Prefer Manual over Disabled unless risk <= 2 and explicitly vetted.
        foreach (var (profile, start) in entry.Recommend)
        {
            if (start == ServiceStartType.Disabled && entry.Risk > 2 && entry.TouchPolicy == TouchPolicy.Allow)
            {
                errors.Add(
                    $"Profile '{profile}' recommends Disabled at risk {entry.Risk}; prefer Manual unless vetted.");
            }
        }

        if (entry.TouchPolicy == TouchPolicy.Never && entry.Recommend.Count > 0)
        {
            errors.Add("touch_policy 'never' must not include recommend changes.");
        }

        if (entry.Risk == 0 && entry.TouchPolicy != TouchPolicy.Never)
        {
            errors.Add("risk 0 entries must use touch_policy: never.");
        }

        return new CatalogValidationResult(errors);
    }

    [GeneratedRegex(@"^svc\.[a-z0-9][a-z0-9_.-]*$")]
    private static partial Regex IdRegex();

    [GeneratedRegex(@"^>=\d+$")]
    private static partial Regex BuildsRegex();
}

public sealed class CatalogValidationResult
{
    public CatalogValidationResult(IReadOnlyList<string> errors)
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Errors.Count == 0;
}
