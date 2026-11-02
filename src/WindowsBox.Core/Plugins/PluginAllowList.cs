namespace WindowsBox.Core.Plugins;

/// <summary>
/// Allow-list of plugin ids that ship in-repo under <c>plugins/</c>.
/// Anything not listed (or present on the deny-list) cannot be loaded.
/// </summary>
public sealed class PluginAllowList
{
    private readonly HashSet<string> _allowed;
    private readonly HashSet<string> _denied;

    public PluginAllowList(IEnumerable<string> allowedIds, IEnumerable<string>? deniedIds = null)
    {
        _allowed = new HashSet<string>(allowedIds, StringComparer.OrdinalIgnoreCase);
        _denied = new HashSet<string>(deniedIds ?? [], StringComparer.OrdinalIgnoreCase);
    }

    public bool IsAllowed(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
        {
            return false;
        }

        if (_denied.Contains(pluginId))
        {
            return false;
        }

        return _allowed.Contains(pluginId);
    }

    public IReadOnlyCollection<string> AllowedIds => _allowed;

    public IReadOnlyCollection<string> DeniedIds => _denied;

    public static PluginAllowList FromDenyListFile(IEnumerable<string> allowedIds, string? denyListPath)
    {
        var denied = new List<string>();
        if (!string.IsNullOrWhiteSpace(denyListPath) && File.Exists(denyListPath))
        {
            foreach (var line in File.ReadAllLines(denyListPath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith(';'))
                {
                    continue;
                }

                denied.Add(trimmed);
            }
        }

        return new PluginAllowList(allowedIds, denied);
    }
}
