using System.Text;
using WindowsBox.Core.Engine.Preview;

namespace WindowsBox.Core.Presentation;

/// <summary>
/// Formats a <see cref="PreviewPlan"/> for the Fluent UI card list and CLI.
/// Kept in Core so WinUI and tests share one presentation contract.
/// </summary>
public static class PreviewDiffPresenter
{
    public static string Headline(PreviewPlan plan) =>
        string.IsNullOrWhiteSpace(plan.DiffSummary) ? "No changes." : plan.DiffSummary;

    public static IReadOnlyList<PreviewCardModel> ToCards(PreviewPlan plan) =>
        plan.Changes.Select(c => new PreviewCardModel
        {
            ItemId = c.ItemId,
            Title = c.Display,
            Subtitle = c.ServiceName,
            Diff = $"{c.Current} → {c.Target}",
            Risk = c.Risk,
            RiskLabel = RiskLabel(c.Risk),
            ExpectedGainMb = c.ExpectedGainMb,
            RequiresConfirm = c.RequiresConfirm,
            Breaks = c.Breaks,
            HonestyLabel = c.HonestyLabel,
        }).ToList();

    public static string RiskLabel(int risk) => risk switch
    {
        0 => "Never touched",
        1 => "Safe",
        2 => "Situational",
        3 => "Advanced",
        4 => "Dangerous",
        _ => $"Risk {risk}",
    };

    public static string FormatPlainText(PreviewPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Headline(plan));
        foreach (var card in ToCards(plan))
        {
            sb.AppendLine($"- [{card.RiskLabel}] {card.Title}: {card.Diff} (~{card.ExpectedGainMb} MB)");
        }

        return sb.ToString().TrimEnd();
    }
}

public sealed class PreviewCardModel
{
    public required string ItemId { get; init; }

    public required string Title { get; init; }

    public required string Subtitle { get; init; }

    public required string Diff { get; init; }

    public required int Risk { get; init; }

    public required string RiskLabel { get; init; }

    public required int ExpectedGainMb { get; init; }

    public bool RequiresConfirm { get; init; }

    public IReadOnlyList<string> Breaks { get; init; } = [];

    public string? HonestyLabel { get; init; }
}
