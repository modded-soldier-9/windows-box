using System.Text;

namespace WindowsBox.Core.Network;

/// <summary>
/// CLI/UI façade for the one-shot privacy check. Capture → report → exit.
/// </summary>
public sealed class NetworkPrivacyModule
{
    private readonly INetworkPrivacyCapture _capture;

    public NetworkPrivacyModule(INetworkPrivacyCapture? capture = null)
    {
        _capture = capture ?? new ActiveTcpNetworkPrivacyCapture();
    }

    public NetworkPrivacyReport Run(NetworkPrivacyOptions? options = null) =>
        _capture.Capture(options);

    public string FormatPlainText(NetworkPrivacyReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Windows Box network privacy check — {report.CapturedAt:u}");
        sb.AppendLine(report.Summary);
        sb.AppendLine("One-shot snapshot; process exits after this report. No resident monitor.");
        sb.AppendLine();
        foreach (var e in report.Entries)
        {
            var tag = e.LooksMicrosoft ? "msft" : "other";
            sb.AppendLine(
                $"[{tag}] {e.RemoteEndPoint,-22} {e.RemoteHost ?? "-"}  state={e.State}  pid={e.OwningProcessId?.ToString() ?? "?"}");
        }

        return sb.ToString().TrimEnd();
    }
}
