using System.Text;
using WindowsBox.Core.Engine;
using WindowsBox.Core.Engine.Preview;

namespace WindowsBox.Core.Presentation;

/// <summary>
/// Formats a dry-run so UI and CLI show exactly what would change — no mutations.
/// </summary>
public static class DryRunPresenter
{
    public static string Format(PreviewPlan plan, ApplyResult? dryRunResult = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Dry-run — profile {plan.Profile}");
        sb.AppendLine(plan.DiffSummary);
        if (!string.IsNullOrWhiteSpace(plan.HonestyNote))
        {
            sb.AppendLine(plan.HonestyNote);
        }

        sb.AppendLine();
        sb.AppendLine("Planned changes (not applied):");
        if (plan.Changes.Count == 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var change in plan.Changes)
            {
                sb.AppendLine($"  • {change.DiffLine}");
                if (change.Breaks.Count > 0)
                {
                    sb.AppendLine($"      breaks: {string.Join("; ", change.Breaks)}");
                }

                if (change.RequiresConfirm)
                {
                    sb.AppendLine("      requires explicit confirm (risk ≥3 or Disabled)");
                }
            }
        }

        if (plan.Skipped.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"Skipped ({plan.Skipped.Count}):");
            foreach (var skip in plan.Skipped.Take(25))
            {
                sb.AppendLine($"  • {skip.ServiceName}: {skip.Reason}");
            }
        }

        if (dryRunResult is not null)
        {
            sb.AppendLine();
            sb.AppendLine($"Journal snapshot id: {dryRunResult.Journal.Id} (status dry-run; no SCM writes).");
        }

        return sb.ToString().TrimEnd();
    }
}
