namespace Zest.Domain;

public static class GameStateFactory
{
    /// <summary>Creates the canonical empty run state for a root seed.</summary>
    public static GameState CreateEmpty(ulong rootSeed, string configVersion = "unversioned")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configVersion);
        return new GameState(rootSeed, configVersion);
    }

    public static DayCycle.DayBoundarySnapshot CreateDayBoundarySnapshot(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var interventions = state.Operations.Interventions;
        var batches = interventions.PreparedBatches
            .Select(b => new DayCycle.PreparedBatchSnapshot(b.BatchId, b.ProductId, b.InitialServings, b.RemainingServings, b.PreparedAtSimTime, b.ExpiresAtSimTime, b.PrepaidCostMinor))
            .ToArray();
        return new(DayCycle.DayBoundarySnapshot.CurrentSaveVersion, state.RootSeed, state.ConfigVersion, state.DayIndex,
            state.Business.OpeningCashMinorUnits, state.Business.CashMinorUnits,
            state.DayCycle.PlannedPriceMinor, state.DayCycle.PlannedBatchSize,
            state.Progression.Reputation,
            state.Progression.Unlocks.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            state.Inventory.Lots.Values.OrderBy(lot => lot.LotId, StringComparer.Ordinal)
                .Select(lot => new DayCycle.InventoryLotSnapshot(lot.LotId, lot.IngredientId, lot.RemainingQuantity, lot.ExpiresOn)).ToArray(),
            state.Business.Ledger.ToArray(),
            SimTime: state.SimTime,
            Phase: state.DayCycle.Phase.ToString(),
            RemainingOpeningServings: state.DayCycle.RemainingOpeningServings,
            PreparedBatches: batches,
            CurrentPrices: interventions.CurrentPrices.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal),
            DisabledUntil: interventions.DisabledUntil.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal),
            RushMenuIds: interventions.RushMenuProductIds.ToArray());
    }

    public static GameState RestoreDayBoundary(DayCycle.DayBoundarySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.SaveVersion is < 1 or > DayCycle.DayBoundarySnapshot.CurrentSaveVersion)
            throw new NotSupportedException($"Unsupported save version '{snapshot.SaveVersion}'.");
        GameState state = CreateEmpty(snapshot.RootSeed, snapshot.ConfigVersion);
        state.DayIndex = snapshot.DayIndex;
        state.DayCycle.PlannedPriceMinor = snapshot.PlannedPriceMinor;
        state.DayCycle.PlannedBatchSize = snapshot.PlannedBatchSize;
        state.Progression.Restore(snapshot.Unlocks);
        state.Progression.RestoreReputation(snapshot.SaveVersion == 1 ? 50 : snapshot.Reputation);
        state.Business.Restore(snapshot.OpeningCashMinor, snapshot.CashMinor, snapshot.Ledger);
        Inventory.InventoryService inventory = new(state.Inventory);
        foreach (DayCycle.InventoryLotSnapshot lot in snapshot.Inventory)
            inventory.ReceiveLot(lot.LotId, lot.IngredientId, lot.RemainingQuantity, lot.ExpiresOn);
        if (snapshot.SaveVersion >= 3)
        {
            state.SimTime = snapshot.SimTime;
            state.DayCycle.Phase = Enum.TryParse<DayCycle.DayPhase>(snapshot.Phase, out var phase) ? phase : DayCycle.DayPhase.MorningBrief;
            state.DayCycle.RemainingOpeningServings = snapshot.RemainingOpeningServings;
            var batches = (snapshot.PreparedBatches ?? Array.Empty<DayCycle.PreparedBatchSnapshot>())
                .Select(b => Operations.LiveInterventionState.HydrateBatch(b.BatchId, b.ProductId, b.InitialServings, b.RemainingServings, b.PreparedAtSimTime, b.ExpiresAtSimTime, b.PrepaidCostMinor));
            state.Operations.Interventions.RestoreForQuicksave(batches, snapshot.CurrentPrices, snapshot.DisabledUntil, snapshot.RushMenuIds);
        }
        return state;
    }
}
