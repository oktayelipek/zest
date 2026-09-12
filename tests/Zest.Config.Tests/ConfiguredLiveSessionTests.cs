using Zest.Domain.DayCycle;
using Zest.Domain;
using Zest.Domain.Economy;
using Xunit;
using Zest.Persistence;
using Zest.Domain.Operations;
using Zest.Domain.Inventory;
using Zest.Domain.Customers;

namespace Zest.Config.Tests;

public sealed class ConfiguredLiveSessionTests
{
    [Fact]
    public void Configured_runner_completes_real_sales_without_overselling()
    {
        ContentConfig config = new ConfigLoader().Load(ConfigRoot());
        GameState state = ConfigGameStateFactory.CreateEmpty(8675309, config);
        LiveDayRunner runner = ConfigLiveSessionFactory.Create(state, config, new DateOnly(2026, 9, 10));
        DayCommandProcessor commands = new(state, runner);
        commands.Execute(new AddBerryToMenuCommand());

        commands.Execute(new PlanDayCommand(350, 2));
        commands.Execute(new StartLiveCommand());
        Assert.NotNull(runner.WeatherId);
        Assert.NotNull(runner.TemperatureC);
        commands.Execute(new AdvanceLiveCommand(10 * 60 * 60));
        DaySessionSnapshot report = commands.Execute(new CloseDayCommand());

        Assert.InRange(report.SalesCount, 1, 40);
        Assert.Equal(report.SalesCount, state.Business.Ledger.Count(entry => entry.Type == LedgerEntryType.Sale));
        Assert.Equal(report.SalesCount, state.Business.Ledger.Count(entry => entry.Type == LedgerEntryType.CostOfGoodsSold));
        Assert.InRange(report.StockOnHand, 0, 2);
        Assert.All(state.Inventory.Lots.Values, lot => Assert.True(lot.RemainingQuantity >= 0));
        Assert.Contains(state.Operations.Orders.Values, order => order.Status == OrderStatus.Served && order.RecipeVersionId.RecipeId == "berry");
        Assert.True(report.QueueLossCount > 0);
        Assert.InRange(state.Progression.Reputation, 0, 100);
    }

    [Fact]
    public void Opening_half_hour_produces_a_visible_first_sale_for_the_default_seed()
    {
        ContentConfig config = new ConfigLoader().Load(ConfigRoot());
        GameState state = ConfigGameStateFactory.CreateEmpty(8675309, config);
        LiveDayRunner runner = ConfigLiveSessionFactory.Create(state, config, new DateOnly(2026, 9, 10));
        DayCommandProcessor commands = new(state, runner);
        commands.Execute(new PlanDayCommand(350, 12));
        commands.Execute(new StartLiveCommand());
        commands.Execute(new AdvanceLiveCommand(30 * 60));

        Assert.Contains(state.Business.Ledger, entry => entry.Type == LedgerEntryType.Sale);
    }

    [Fact]
    public void Configured_extra_prep_consumes_raw_stock_and_records_expiry_waste()
    {
        ContentConfig config = new ConfigLoader().Load(ConfigRoot());
        GameState state = ConfigGameStateFactory.CreateEmpty(9, config);
        LiveDayRunner runner = ConfigLiveSessionFactory.Create(state, config, new DateOnly(2026, 9, 10));
        DayCommandProcessor commands = new(state, runner);
        commands.Execute(new PlanDayCommand(350, 1));
        commands.Execute(new StartLiveCommand());
        decimal lemonsBefore = state.Inventory.Quantities["lemon"];

        commands.Execute(new PrepareExtraBatchCommand("classic", 2, 30));
        Assert.Equal(lemonsBefore - 2, state.Inventory.Quantities["lemon"]);
        commands.Execute(new AdvanceLiveCommand(31));

        Assert.Contains(state.Business.Ledger, entry => entry.Type == LedgerEntryType.Waste && entry.Amount.MinorUnits == -120 && !entry.AffectsCash);
    }

    [Fact]
    public void Closing_cancels_open_work_and_releases_its_inventory_holds()
    {
        ContentConfig config = new ConfigLoader().Load(ConfigRoot());
        GameState state = ConfigGameStateFactory.CreateEmpty(8675309, config);
        LiveDayRunner runner = ConfigLiveSessionFactory.Create(state, config, new DateOnly(2026, 9, 10));
        DayCommandProcessor commands = new(state, runner);
        commands.Execute(new PlanDayCommand(350, 12));
        commands.Execute(new StartLiveCommand());

        DaySessionSnapshot report = commands.Execute(new CloseDayCommand());

        Assert.Empty(state.Operations.ActiveOrderIds);
        Assert.DoesNotContain(state.Inventory.Reservations.Values, reservation => reservation.Status == ReservationStatus.Active);
        Assert.True(report.QueueLossCount > 0);
        Assert.Contains(state.Operations.LossEvents, loss => loss.Reason == LostSaleReason.ClosingTime);
    }

    [Fact]
    public void Same_seed_and_elapsed_time_produce_identical_results_at_different_frame_intervals()
    {
        RunResult oneJump = RunDay([10 * 60 * 60]);
        RunResult frameLike = RunDay(Enumerable.Repeat(137L, 262).Append(106L));

        Assert.Equal(oneJump.Report, frameLike.Report);
        Assert.Equal(oneJump.Ledger, frameLike.Ledger);
        Assert.Equal(oneJump.Inventory, frameLike.Inventory);
        Assert.Equal(oneJump.Losses, frameLike.Losses);
    }

    [Fact]
    public void Live_reprice_is_used_by_customers_created_after_the_change()
    {
        ContentConfig config = new ConfigLoader().Load(ConfigRoot());
        GameState state = ConfigGameStateFactory.CreateEmpty(8675309, config);
        LiveDayRunner runner = ConfigLiveSessionFactory.Create(state, config, new DateOnly(2026, 9, 10));
        DayCommandProcessor commands = new(state, runner);
        commands.Execute(new PlanDayCommand(350, 12));
        commands.Execute(new StartLiveCommand());
        commands.Execute(new ChangeLivePriceCommand("classic", 400));
        commands.Execute(new AdvanceLiveCommand(4 * 60 * 60));

        Assert.Equal(400, runner.CurrentPrice("classic"));
        Assert.Contains(state.Business.Ledger,
            entry => entry.Type == LedgerEntryType.Sale && entry.Amount.MinorUnits == 400);
    }

    [Fact]
    public void Emergency_restock_charges_once_and_becomes_sellable_on_delivery()
    {
        ContentConfig config = new ConfigLoader().Load(ConfigRoot());
        GameState state = ConfigGameStateFactory.CreateEmpty(8675309, config);
        LiveDayRunner runner = ConfigLiveSessionFactory.Create(state, config, new DateOnly(2026, 9, 10));
        DayCommandProcessor commands = new(state, runner);
        commands.Execute(new PlanDayCommand(350, 1));
        commands.Execute(new StartLiveCommand());
        commands.Execute(new RequestEmergencyRestockCommand("classic", 8, 120, 50, 1.75m));

        Assert.Contains(state.Business.Ledger, entry => entry.Type == LedgerEntryType.Supply && entry.Amount.MinorUnits == -700);
        Assert.Empty(state.Operations.Interventions.PreparedBatches);
        commands.Execute(new AdvanceLiveCommand(120));

        PreparedBatchState delivery = Assert.Single(state.Operations.Interventions.PreparedBatches);
        Assert.Equal(8, delivery.RemainingServings);
        Assert.Equal(700, delivery.PrepaidCostMinor);
        Assert.True(runner.SellableServings("classic") >= 8);
    }

    [Fact]
    public void Three_consecutive_days_keep_cash_and_return_to_a_playable_morning()
    {
        ContentConfig config = new ConfigLoader().Load(ConfigRoot());
        GameState state = ConfigGameStateFactory.CreateEmpty(8675309, config);
        LiveDayRunner runner = ConfigLiveSessionFactory.Create(state, config, new DateOnly(2026, 9, 10));
        DayCommandProcessor commands = new(state, runner);

        for (int day = 0; day < 3; day++)
        {
            commands.Execute(new PlanDayCommand(350, 12));
            commands.Execute(new StartLiveCommand());
            commands.Execute(new AdvanceLiveCommand(10 * 60 * 60));
            DaySessionSnapshot report = commands.Execute(new CloseDayCommand());
            Assert.NotNull(report.Report);
            commands.Execute(new StartNextDayCommand());
        }

        Assert.Equal(3, state.DayIndex);
        Assert.Equal(DayPhase.MorningBrief, state.DayCycle.Phase);
        Assert.True(state.Business.CashMinorUnits > 0);
        Assert.NotEmpty(state.Business.Ledger);
    }

    [Fact]
    public void Juicer_and_cooler_change_the_next_live_day_operations()
    {
        ContentConfig config = new ConfigLoader().Load(ConfigRoot());
        GameState state = ConfigGameStateFactory.CreateEmpty(8675309, config);
        LiveDayRunner runner = ConfigLiveSessionFactory.Create(state, config, new DateOnly(2026, 9, 10));
        DayCommandProcessor commands = new(state, runner);
        commands.Execute(new StartLiveCommand());
        commands.Execute(new CloseDayCommand());
        commands.Execute(new PurchaseElectricJuicerCommand());
        commands.Execute(new PurchaseBiggerCoolerCommand());
        commands.Execute(new StartNextDayCommand());
        commands.Execute(new PlanDayCommand(350, 12));
        commands.Execute(new StartLiveCommand());

        Assert.Contains(state.Operations.Tasks.Values, task => task.Step.DurationSeconds == 20);
        commands.Execute(new PrepareExtraBatchCommand("classic", 1, 30));
        PreparedBatchState batch = Assert.Single(state.Operations.Interventions.PreparedBatches);
        Assert.Equal(60, batch.ExpiresAtSimTime - batch.PreparedAtSimTime);
    }

    private static RunResult RunDay(IEnumerable<long> advances)
    {
        ContentConfig config = new ConfigLoader().Load(ConfigRoot());
        GameState state = ConfigGameStateFactory.CreateEmpty(8675309, config);
        LiveDayRunner runner = ConfigLiveSessionFactory.Create(state, config, new DateOnly(2026, 9, 10));
        DayCommandProcessor commands = new(state, runner);
        commands.Execute(new PlanDayCommand(350, 12));
        commands.Execute(new StartLiveCommand());
        foreach (long seconds in advances) commands.Execute(new AdvanceLiveCommand(seconds));
        DaySessionSnapshot report = commands.Execute(new CloseDayCommand());
        return new(report, state.Business.Ledger.ToArray(), state.Inventory.Quantities.OrderBy(pair => pair.Key).ToArray(), state.Operations.LossEvents.ToArray());
    }

    private sealed record RunResult(DaySessionSnapshot Report, IReadOnlyList<LedgerEntry> Ledger,
        IReadOnlyList<KeyValuePair<string, decimal>> Inventory, IReadOnlyList<OrderLossEvent> Losses);

    [Fact]
    public void Morning_boundary_save_round_trips_cash_ledger_and_remaining_inventory()
    {
        ContentConfig config = new ConfigLoader().Load(ConfigRoot());
        GameState state = ConfigGameStateFactory.CreateEmpty(8675309, config);
        LiveDayRunner runner = ConfigLiveSessionFactory.Create(state, config, new DateOnly(2026, 9, 10));
        DayCommandProcessor commands = new(state, runner);
        commands.Execute(new PlanDayCommand(350, 2));
        commands.Execute(new StartLiveCommand());
        commands.Execute(new AdvanceLiveCommand(10 * 60 * 60));
        commands.Execute(new CloseDayCommand());
        commands.Execute(new StartNextDayCommand());
        string path = Path.Combine(Path.GetTempPath(), $"zest-{Guid.NewGuid():N}.json");
        try
        {
            DayBoundarySaveService saves = new();
            saves.Save(path, state);
            GameState restored = saves.Load(path);
            Assert.Equal(state.DayIndex, restored.DayIndex);
            Assert.Equal(state.Business.CashMinorUnits, restored.Business.CashMinorUnits);
            Assert.Equal(state.Business.Ledger, restored.Business.Ledger);
            Assert.Equal(state.Inventory.Quantities, restored.Inventory.Quantities);
            Assert.Equal(state.Progression.Reputation, restored.Progression.Reputation);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Berry_stays_off_menu_until_unlocked()
    {
        ContentConfig config = new ConfigLoader().Load(ConfigRoot());
        GameState state = ConfigGameStateFactory.CreateEmpty(8675309, config);
        LiveDayRunner runner = ConfigLiveSessionFactory.Create(state, config, new DateOnly(2026, 9, 10));
        DayCommandProcessor commands = new(state, runner);
        commands.Execute(new PlanDayCommand(350, 12));
        commands.Execute(new StartLiveCommand());
        commands.Execute(new AdvanceLiveCommand(10 * 60 * 60));
        commands.Execute(new CloseDayCommand());

        Assert.DoesNotContain(state.Operations.Orders.Values,
            o => o.RecipeVersionId.RecipeId == "berry");
        Assert.Contains(state.Business.Ledger, e => e.Type == LedgerEntryType.Sale);
    }

    [Fact]
    public void A_default_day_produces_both_products_and_lost_customers_for_ui_juice()
    {
        ContentConfig config = new ConfigLoader().Load(ConfigRoot());
        GameState state = ConfigGameStateFactory.CreateEmpty(8675309, config);
        LiveDayRunner runner = ConfigLiveSessionFactory.Create(state, config, new DateOnly(2026, 9, 10));
        DayCommandProcessor commands = new(state, runner);
        commands.Execute(new AddBerryToMenuCommand());
        commands.Execute(new PlanDayCommand(350, 12));
        commands.Execute(new StartLiveCommand());
        commands.Execute(new AdvanceLiveCommand(10 * 60 * 60));
        commands.Execute(new CloseDayCommand());

        var sales = state.Business.Ledger.Where(e => e.Type == LedgerEntryType.Sale).ToArray();
        Assert.NotEmpty(sales);
        Assert.Contains(state.Operations.Orders.Values,
            o => o.Status == OrderStatus.Served && o.RecipeVersionId.RecipeId == "classic");
        Assert.Contains(state.Operations.Orders.Values,
            o => o.Status == OrderStatus.Served && o.RecipeVersionId.RecipeId == "berry");
        Assert.NotEmpty(state.Operations.LossEvents);
        Assert.Contains(state.Operations.LossEvents,
            l => l.Reason is LostSaleReason.OutsideOption
                        or LostSaleReason.PriceTooHigh
                        or LostSaleReason.PoorProductFit
                        or LostSaleReason.QueueAbandonment
                        or LostSaleReason.ClosingTime);
    }

    private static string ConfigRoot() => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "config"));
}
