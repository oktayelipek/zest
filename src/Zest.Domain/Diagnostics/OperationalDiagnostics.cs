using Zest.Domain.Customers;
using Zest.Domain.Economy;
using Zest.Domain.Operations;

namespace Zest.Domain.Diagnostics;

public enum DiagnosticSeverity { Calm, Watch, Critical }
public sealed record DiagnosticSignal(string Area, DiagnosticSeverity Severity, string Headline, string Evidence);
public sealed record OperationalDiagnosticSnapshot(IReadOnlyList<DiagnosticSignal> Signals, IReadOnlyList<string> LostCustomerFeed, string YesterdayComparison, string Bottleneck);

/// <summary>Read-only interpretation of authoritative facts; observation never mutates outcomes.</summary>
public sealed class OperationalDiagnosticService
{
    public OperationalDiagnosticSnapshot Observe(GameState state, string productId = "classic")
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        int queue = state.Operations.ActiveOrderIds.Count;
        int stock = state.DayCycle.RemainingOpeningServings + state.Operations.Interventions.PreparedBatches.Sum(batch => batch.RemainingServings);
        long currentPrice = state.Operations.Interventions.CurrentPrices.GetValueOrDefault(productId, state.DayCycle.PlannedPriceMinor);
        LivePriceChange? latestPrice = state.Operations.Interventions.PriceChanges.LastOrDefault(change => change.ProductId == productId);
        int priceLosses = state.Operations.LossEvents.Count(loss => loss.DayIndex == state.DayIndex && loss.Reason == LostSaleReason.PriceTooHigh);
        int fitLosses = state.Operations.LossEvents.Count(loss => loss.DayIndex == state.DayIndex && loss.Reason == LostSaleReason.PoorProductFit);
        List<DiagnosticSignal> signals =
        [
            new("QUEUE", queue >= 5 ? DiagnosticSeverity.Critical : queue >= 3 ? DiagnosticSeverity.Watch : DiagnosticSeverity.Calm,
                queue >= 5 ? "Line is becoming the dominant pressure" : queue >= 3 ? "Waiting line is building" : "Flow is currently manageable", $"{queue} active orders are competing for service."),
            new("PRICE", priceLosses > 1 || latestPrice is { NewPriceMinor: var next, PreviousPriceMinor: var prior } && next >= prior * 1.15m ? DiagnosticSeverity.Watch : DiagnosticSeverity.Calm,
                priceLosses > 1 ? "Guests are resisting the offer" : "Price resistance is not dominant", $"Classic is {currentPrice / 100m:C}; {priceLosses} price-related losses today."),
            new("STOCK", stock <= 2 ? DiagnosticSeverity.Critical : stock <= 6 ? DiagnosticSeverity.Watch : DiagnosticSeverity.Calm,
                stock <= 2 ? "Sell-out risk is immediate" : stock <= 6 ? "Prepared stock is thinning" : "Prepared stock covers the visible line", $"{stock} Classic servings are currently available or prepared."),
            new("PRODUCT FIT", fitLosses > 1 ? DiagnosticSeverity.Watch : DiagnosticSeverity.Calm,
                fitLosses > 1 ? "The menu is missing repeated preferences" : "Product fit is not the dominant loss", $"{fitLosses} product-fit losses were observed today."),
        ];
        string[] lost = state.Operations.LossEvents.Where(loss => loss.DayIndex == state.DayIndex).OrderByDescending(loss => loss.SimTime).Take(4)
            .Select(loss => $"{loss.Reason} · waited {loss.WaitSeconds}s").ToArray();
        if (lost.Length == 0) lost = ["No lost customers observed yet."];
        int yesterday = state.DayIndex - 1;
        string comparison = yesterday < 0 ? "No prior-day baseline yet." :
            $"Yesterday: {state.Business.Ledger.Count(entry => entry.DayIndex == yesterday && entry.Type == LedgerEntryType.Sale)} sales, {state.Operations.LossEvents.Count(loss => loss.DayIndex == yesterday)} observed losses.";
        OperationalResourceState? constrained = state.Operations.Resources.Values.Where(resource => resource.ActiveTaskIds.Count >= resource.Capacity)
            .OrderByDescending(resource => resource.ActiveTaskIds.Count).FirstOrDefault();
        string bottleneck = constrained is null ? "No resource is saturated right now." : $"{constrained.Key} is fully occupied ({constrained.ActiveTaskIds.Count}/{constrained.Capacity}).";
        return new(signals, lost, comparison, bottleneck);
    }
}
