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
        if (state.DayCycle.Phase != DayCycle.DayPhase.MorningBrief)
            throw new InvalidOperationException("Runs can be saved only at the morning boundary.");
        return new(DayCycle.DayBoundarySnapshot.CurrentSaveVersion, state.RootSeed, state.ConfigVersion, state.DayIndex,
            state.Business.OpeningCashMinorUnits, state.Business.CashMinorUnits,
            state.DayCycle.PlannedPriceMinor, state.DayCycle.PlannedBatchSize,
            state.Progression.Reputation,
            state.Progression.Unlocks.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            state.Inventory.Lots.Values.OrderBy(lot => lot.LotId, StringComparer.Ordinal)
                .Select(lot => new DayCycle.InventoryLotSnapshot(lot.LotId, lot.IngredientId, lot.RemainingQuantity, lot.ExpiresOn)).ToArray(),
            state.Business.Ledger.ToArray());
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
        return state;
    }
}
