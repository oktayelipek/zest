using Zest.Domain.Economy;
using Zest.Domain.Products;
using Xunit;

namespace Zest.Domain.Tests;

public sealed class EconomyLedgerTests
{
    [Fact]
    public void Served_order_posts_sale_and_cogs_exactly_once()
    {
        BusinessState state = new();
        EconomyService economy = new(state);
        economy.InitializeOpeningCash(new Money(1_000));

        Assert.True(economy.PostServedOrder("order-1", Recipe(), 10, 0));
        Assert.False(economy.PostServedOrder("order-1", Recipe(), 11, 0));

        Assert.Collection(state.Ledger,
            sale => { Assert.Equal(LedgerEntryType.Sale, sale.Type); Assert.Equal(350, sale.Amount.MinorUnits); },
            cogs => { Assert.Equal(LedgerEntryType.CostOfGoodsSold, cogs.Type); Assert.Equal(-60, cogs.Amount.MinorUnits); });
        Assert.Equal(1_290, state.CashMinorUnits);
    }

    [Fact]
    public void Revenue_profit_and_cash_remain_distinct()
    {
        BusinessState state = new();
        EconomyService economy = new(state);
        economy.InitializeOpeningCash(new Money(5_000));
        economy.PostServedOrder("order-1", Recipe(), 10, 0);
        economy.PostExpense(LedgerEntryType.Labor, new Money(100), "shift-1", 20, 0);
        economy.PostExpense(LedgerEntryType.Rent, new Money(50), "day-0", 0, 0, affectsCash: false);

        FinancialSnapshot day = economy.ProjectDay(0);

        Assert.Equal(350, day.Revenue.MinorUnits);
        Assert.Equal(140, day.Profit.MinorUnits);
        Assert.Equal(5_190, state.CashMinorUnits);
        Assert.True(economy.ReconcileCash().IsBalanced);
    }

    [Fact]
    public void Ledger_is_exposed_as_read_only_financial_history()
    {
        BusinessState state = new();
        EconomyService economy = new(state);
        economy.PostExpense(LedgerEntryType.Waste, new Money(20), "spoiled-lemon", 1, 0);

        Assert.IsAssignableFrom<IReadOnlyList<LedgerEntry>>(state.Ledger);
        Assert.False(state.Ledger is List<LedgerEntry>);
        Assert.Equal(1, state.Ledger[0].Sequence);
    }

    [Fact]
    public void One_thousand_seeded_runs_preserve_financial_invariants()
    {
        for (int seed = 0; seed < 1_000; seed++)
        {
            Random random = new(seed);
            BusinessState state = new();
            EconomyService economy = new(state);
            long opening = random.Next(10_000, 100_000);
            economy.InitializeOpeningCash(new Money(opening));

            int served = random.Next(1, 30);
            for (int order = 0; order < served; order++)
            {
                string id = $"{seed}-{order}";
                Assert.True(economy.PostServedOrder(id, Recipe(), order, 0));
                Assert.False(economy.PostServedOrder(id, Recipe(), order, 0));
            }
            economy.PostExpense(LedgerEntryType.Waste, new Money(random.Next(0, 500)), "waste", 100, 0);
            economy.PostExpense(LedgerEntryType.Labor, new Money(random.Next(0, 2_000)), "labor", 200, 0);
            economy.PostExpense(LedgerEntryType.Rent, new Money(random.Next(0, 1_000)), "rent", 0, 0);

            FinancialSnapshot snapshot = economy.ProjectDay(0);
            Assert.Equal(served * 350, snapshot.Revenue.MinorUnits);
            Assert.Equal(served * 60, snapshot.Cogs.MinorUnits);
            Assert.True(economy.ReconcileCash().IsBalanced);
            Assert.Equal(opening + state.Ledger.Where(entry => entry.AffectsCash).Sum(entry => entry.Amount.MinorUnits), state.CashMinorUnits);
        }
    }

    private static RecipeVersion Recipe() => new(
        new RecipeVersionId("classic", 1),
        [new RecipeComponent("lemon", 1, "each")],
        350,
        new ProductProfile(0.5m, 0.5m, 0.5m, 0.5m),
        60,
        30);
}
