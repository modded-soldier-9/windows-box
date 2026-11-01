using System.Globalization;
using WindowsBox.Core.Catalog.Models;
using YamlDotNet.RepresentationModel;

namespace WindowsBox.Core.Catalog;

public sealed class ServiceCatalogLoader
{
    private readonly ServiceCatalogValidator _validator;

    public ServiceCatalogLoader(ServiceCatalogValidator? validator = null)
    {
        _validator = validator ?? new ServiceCatalogValidator();
    }

    public CatalogDocument LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new CatalogLoadException($"Catalog directory not found: {directory}");
        }

        var files = Directory.EnumerateFiles(directory, "*.yaml", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(directory, "*.yml", SearchOption.TopDirectoryOnly))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            throw new CatalogLoadException($"No YAML files found in {directory}");
        }

        var entries = new List<ServiceCatalogEntry>(files.Count);
        var errors = new List<string>();

        foreach (var file in files)
        {
            try
            {
                var entry = LoadFile(file);
                var validation = _validator.Validate(entry);
                if (!validation.IsValid)
                {
                    errors.AddRange(validation.Errors.Select(e => $"{Path.GetFileName(file)}: {e}"));
                    continue;
                }

                entries.Add(entry);
            }
            catch (CatalogLoadException ex)
            {
                if (ex.Errors.Count > 0)
                {
                    errors.AddRange(ex.Errors.Select(e => $"{Path.GetFileName(file)}: {e}"));
                }
                else
                {
                    errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        var duplicates = entries
            .GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var id in duplicates)
        {
            errors.Add($"Duplicate service id: {id}");
        }

        var nameDupes = entries
            .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var name in nameDupes)
        {
            errors.Add($"Duplicate service name: {name}");
        }

        if (errors.Count > 0)
        {
            throw new CatalogLoadException(
                $"Catalog validation failed with {errors.Count} error(s).",
                new AggregateException(errors.Select(e => new Exception(e))))
            {
                Errors = errors,
            };
        }

        return new CatalogDocument { Services = entries };
    }

    public ServiceCatalogEntry LoadFile(string path)
    {
        using var reader = File.OpenText(path);
        var yaml = new YamlStream();
        yaml.Load(reader);

        if (yaml.Documents.Count == 0)
        {
            throw new CatalogLoadException($"Empty YAML document: {path}");
        }

        if (yaml.Documents[0].RootNode is not YamlMappingNode root)
        {
            throw new CatalogLoadException($"Root must be a mapping: {path}");
        }

        var map = ToDictionary(root);

        return new ServiceCatalogEntry
        {
            SchemaVersion = ReadInt(map, "schema_version", required: true),
            Id = ReadString(map, "id", required: true),
            Area = ReadString(map, "area", required: true),
            Name = ReadString(map, "name", required: true),
            Display = ReadString(map, "display", required: true),
            Category = ReadString(map, "category", required: true),
            Description = ReadString(map, "description", required: true),
            Defaults = ReadStartTypeMap(map, "defaults"),
            Builds = ReadString(map, "builds", required: true),
            Risk = ReadInt(map, "risk", required: true),
            Protected = ReadBool(map, "protected"),
            PerUser = ReadBool(map, "per_user"),
            Optional = ReadBool(map, "optional"),
            TouchPolicy = ParseTouchPolicy(ReadString(map, "touch_policy", required: true)),
            Recommend = ReadRecommendMap(map, "recommend"),
            DependsOn = ReadStringList(map, "depends_on"),
            Dependents = ReadStringList(map, "dependents"),
            Breaks = ReadStringList(map, "breaks"),
            Gates = ReadGates(map, "gates"),
            Profiles = ReadProfileList(map, "profiles"),
            Alternatives = ReadAlternatives(map, "alternatives"),
            Notes = ReadString(map, "notes", required: false),
            HonestyLabel = ReadString(map, "honesty_label", required: false),
            Sources = ReadStringList(map, "sources"),
            SourcePath = path,
        };
    }

    private static Dictionary<string, YamlNode> ToDictionary(YamlMappingNode node)
    {
        var dict = new Dictionary<string, YamlNode>(StringComparer.Ordinal);
        foreach (var (keyNode, value) in node.Children)
        {
            if (keyNode is YamlScalarNode key && key.Value is not null)
            {
                dict[key.Value] = value;
            }
        }

        return dict;
    }

    private static string ReadString(Dictionary<string, YamlNode> map, string key, bool required)
    {
        if (!map.TryGetValue(key, out var node))
        {
            if (required)
            {
                throw new CatalogLoadException($"Missing required field '{key}'.");
            }

            return null!;
        }

        if (node is YamlScalarNode scalar)
        {
            return scalar.Value ?? string.Empty;
        }

        throw new CatalogLoadException($"Field '{key}' must be a scalar.");
    }

    private static int ReadInt(Dictionary<string, YamlNode> map, string key, bool required)
    {
        var raw = ReadString(map, key, required);
        if (raw is null)
        {
            return 0;
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new CatalogLoadException($"Field '{key}' must be an integer.");
        }

        return value;
    }

    private static bool ReadBool(Dictionary<string, YamlNode> map, string key)
    {
        if (!map.TryGetValue(key, out var node) || node is not YamlScalarNode scalar || scalar.Value is null)
        {
            return false;
        }

        return scalar.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> ReadStringList(Dictionary<string, YamlNode> map, string key)
    {
        if (!map.TryGetValue(key, out var node))
        {
            return [];
        }

        if (node is YamlSequenceNode seq)
        {
            return seq.Children
                .OfType<YamlScalarNode>()
                .Select(s => s.Value ?? string.Empty)
                .Where(s => s.Length > 0)
                .ToList();
        }

        if (node is YamlScalarNode { Value: null or "" or "~" or "null" })
        {
            return [];
        }

        throw new CatalogLoadException($"Field '{key}' must be a sequence.");
    }

    private static IReadOnlyDictionary<string, ServiceStartType> ReadStartTypeMap(
        Dictionary<string, YamlNode> map,
        string key)
    {
        if (!map.TryGetValue(key, out var node) || node is not YamlMappingNode mapping)
        {
            throw new CatalogLoadException($"Field '{key}' must be a mapping.");
        }

        var result = new Dictionary<string, ServiceStartType>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in ToDictionary(mapping))
        {
            if (v is not YamlScalarNode scalar || scalar.Value is null)
            {
                throw new CatalogLoadException($"Field '{key}.{k}' must be a start type string.");
            }

            result[k] = ParseStartType(scalar.Value);
        }

        return result;
    }

    private static IReadOnlyDictionary<OptimizationProfile, ServiceStartType> ReadRecommendMap(
        Dictionary<string, YamlNode> map,
        string key)
    {
        if (!map.TryGetValue(key, out var node))
        {
            return new Dictionary<OptimizationProfile, ServiceStartType>();
        }

        if (node is YamlScalarNode)
        {
            return new Dictionary<OptimizationProfile, ServiceStartType>();
        }

        if (node is not YamlMappingNode mapping)
        {
            throw new CatalogLoadException($"Field '{key}' must be a mapping.");
        }

        var result = new Dictionary<OptimizationProfile, ServiceStartType>();
        foreach (var (k, v) in ToDictionary(mapping))
        {
            if (v is not YamlScalarNode scalar || scalar.Value is null)
            {
                throw new CatalogLoadException($"Field '{key}.{k}' must be a start type string.");
            }

            result[ParseProfile(k)] = ParseStartType(scalar.Value);
        }

        return result;
    }

    private static ServiceGates ReadGates(Dictionary<string, YamlNode> map, string key)
    {
        if (!map.TryGetValue(key, out var node) || node is not YamlMappingNode mapping)
        {
            return new ServiceGates();
        }

        var child = ToDictionary(mapping);
        return new ServiceGates
        {
            Not = ReadStringList(child, "not"),
            HardwareAny = ReadStringList(child, "hardware_any"),
            FeatureAny = ReadStringList(child, "feature_any"),
        };
    }

    private static IReadOnlyList<OptimizationProfile> ReadProfileList(
        Dictionary<string, YamlNode> map,
        string key)
    {
        return ReadStringList(map, key).Select(ParseProfile).ToList();
    }

    private static IReadOnlyList<PolicyAlternative> ReadAlternatives(
        Dictionary<string, YamlNode> map,
        string key)
    {
        if (!map.TryGetValue(key, out var node) || node is not YamlSequenceNode seq)
        {
            return [];
        }

        var list = new List<PolicyAlternative>();
        foreach (var item in seq.Children)
        {
            if (item is not YamlMappingNode mapping)
            {
                throw new CatalogLoadException("Each alternatives entry must be a mapping.");
            }

            var child = ToDictionary(mapping);
            list.Add(new PolicyAlternative
            {
                Policy = ReadString(child, "policy", required: true),
                Value = ReadString(child, "value", required: false),
                Description = ReadString(child, "description", required: true),
            });
        }

        return list;
    }

    public static ServiceStartType ParseStartType(string value) =>
        value.Trim() switch
        {
            "Automatic" => ServiceStartType.Automatic,
            "AutomaticDelayed" => ServiceStartType.AutomaticDelayed,
            "Manual" => ServiceStartType.Manual,
            "ManualTrigger" => ServiceStartType.ManualTrigger,
            "Disabled" => ServiceStartType.Disabled,
            _ => throw new CatalogLoadException($"Unknown start type '{value}'."),
        };

    public static OptimizationProfile ParseProfile(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "safe" => OptimizationProfile.Safe,
            "balanced" => OptimizationProfile.Balanced,
            "gamer" => OptimizationProfile.Gamer,
            "laptop" => OptimizationProfile.Laptop,
            "office" => OptimizationProfile.Office,
            "privacy" => OptimizationProfile.Privacy,
            "developer" => OptimizationProfile.Developer,
            "minimal" => OptimizationProfile.Minimal,
            _ => throw new CatalogLoadException($"Unknown profile '{value}'."),
        };

    public static TouchPolicy ParseTouchPolicy(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "allow" => TouchPolicy.Allow,
            "leave" => TouchPolicy.Leave,
            "never" => TouchPolicy.Never,
            "policy_only" => TouchPolicy.PolicyOnly,
            _ => throw new CatalogLoadException($"Unknown touch_policy '{value}'."),
        };
}
