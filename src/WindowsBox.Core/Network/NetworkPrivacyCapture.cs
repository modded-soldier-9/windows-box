using System.Net;
using System.Net.NetworkInformation;

namespace WindowsBox.Core.Network;

/// <summary>
/// One-shot "who phoned home" capture. Enumerates active TCP connections and
/// DNS cache hints, writes a report, then the process exits — never a resident
/// sniffer or service.
/// </summary>
public interface INetworkPrivacyCapture
{
    NetworkPrivacyReport Capture(NetworkPrivacyOptions? options = null);
}

public sealed class NetworkPrivacyOptions
{
    public bool IncludeLoopback { get; init; }

    public bool ResolveRemoteHostNames { get; init; }
}

public sealed class NetworkPrivacyReport
{
    public required DateTimeOffset CapturedAt { get; init; }

    public required IReadOnlyList<NetworkPhoneHomeEntry> Entries { get; init; }

    public string Summary =>
        Entries.Count == 0
            ? "No outbound TCP connections observed."
            : $"{Entries.Count} outbound endpoint(s); capture is a snapshot, not continuous monitoring.";
}

public sealed class NetworkPhoneHomeEntry
{
    public required string LocalEndPoint { get; init; }

    public required string RemoteEndPoint { get; init; }

    public string? RemoteHost { get; init; }

    public required string State { get; init; }

    public int? OwningProcessId { get; init; }

    public bool LooksMicrosoft { get; init; }
}

/// <summary>
/// Uses <see cref="IPGlobalProperties"/> — available cross-platform for tests.
/// Process ownership requires Windows IP Helper APIs and is left null off-Windows.
/// </summary>
public sealed class ActiveTcpNetworkPrivacyCapture : INetworkPrivacyCapture
{
    public NetworkPrivacyReport Capture(NetworkPrivacyOptions? options = null)
    {
        options ??= new NetworkPrivacyOptions();
        var props = IPGlobalProperties.GetIPGlobalProperties();
        var entries = new List<NetworkPhoneHomeEntry>();

        foreach (var conn in props.GetActiveTcpConnections())
        {
            if (conn.State is not (TcpState.Established or TcpState.SynSent or TcpState.CloseWait))
            {
                continue;
            }

            if (!options.IncludeLoopback && IPAddress.IsLoopback(conn.RemoteEndPoint.Address))
            {
                continue;
            }

            // Skip purely local listeners / same-host.
            if (conn.RemoteEndPoint.Address.Equals(IPAddress.Any) ||
                conn.RemoteEndPoint.Address.Equals(IPAddress.IPv6Any))
            {
                continue;
            }

            string? host = null;
            if (options.ResolveRemoteHostNames)
            {
                try
                {
                    host = Dns.GetHostEntry(conn.RemoteEndPoint.Address).HostName;
                }
                catch
                {
                    host = null;
                }
            }

            var remote = conn.RemoteEndPoint.ToString();
            entries.Add(new NetworkPhoneHomeEntry
            {
                LocalEndPoint = conn.LocalEndPoint.ToString(),
                RemoteEndPoint = remote,
                RemoteHost = host,
                State = conn.State.ToString(),
                OwningProcessId = null,
                LooksMicrosoft = LooksMicrosoft(host, remote),
            });
        }

        return new NetworkPrivacyReport
        {
            CapturedAt = DateTimeOffset.UtcNow,
            Entries = entries
                .OrderBy(e => e.RemoteEndPoint, StringComparer.OrdinalIgnoreCase)
                .ToList(),
        };
    }

    private static bool LooksMicrosoft(string? host, string remote)
    {
        var blob = $"{host} {remote}".ToLowerInvariant();
        return blob.Contains("microsoft", StringComparison.Ordinal)
               || blob.Contains("windows", StringComparison.Ordinal)
               || blob.Contains("msft", StringComparison.Ordinal)
               || blob.Contains(".msn.", StringComparison.Ordinal)
               || blob.Contains("azure", StringComparison.Ordinal);
    }
}

/// <summary>Deterministic capture for unit tests — never opens sockets.</summary>
public sealed class FakeNetworkPrivacyCapture : INetworkPrivacyCapture
{
    private readonly IReadOnlyList<NetworkPhoneHomeEntry> _entries;

    public FakeNetworkPrivacyCapture(IEnumerable<NetworkPhoneHomeEntry>? entries = null)
    {
        _entries = entries?.ToList() ??
        [
            new NetworkPhoneHomeEntry
            {
                LocalEndPoint = "192.168.1.10:51234",
                RemoteEndPoint = "13.107.42.12:443",
                RemoteHost = "settings-win.data.microsoft.com",
                State = "Established",
                OwningProcessId = 1234,
                LooksMicrosoft = true,
            },
            new NetworkPhoneHomeEntry
            {
                LocalEndPoint = "192.168.1.10:51235",
                RemoteEndPoint = "93.184.216.34:443",
                RemoteHost = "example.com",
                State = "Established",
                OwningProcessId = 2222,
                LooksMicrosoft = false,
            },
        ];
    }

    public NetworkPrivacyReport Capture(NetworkPrivacyOptions? options = null) =>
        new()
        {
            CapturedAt = DateTimeOffset.UtcNow,
            Entries = _entries,
        };
}
