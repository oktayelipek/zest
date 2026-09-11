using System.Reflection;
using Zest.Domain;
using Xunit;

namespace Zest.Domain.Tests;

public sealed class GameStateFactoryTests
{
    [Fact]
    public void CreateEmpty_builds_the_complete_authoritative_state_graph()
    {
        GameState state = GameStateFactory.CreateEmpty(42UL);

        Assert.Equal(GameState.CurrentSchemaVersion, state.SchemaVersion);
        Assert.Equal(42UL, state.RootSeed);
        Assert.Equal(0, state.DayIndex);
        Assert.Equal(0, state.SimTime);
        Assert.NotNull(state.World);
        Assert.NotNull(state.Business);
        Assert.NotNull(state.Customers);
        Assert.NotNull(state.Operations);
        Assert.NotNull(state.Inventory);
        Assert.NotNull(state.Progression);
        Assert.Empty(state.Customers.Customers);
        Assert.Empty(state.Operations.ActiveOrderIds);
        Assert.Empty(state.Inventory.Quantities);
        Assert.Empty(state.Progression.Unlocks);
    }

    [Fact]
    public void CreateEmpty_is_deterministic_for_the_same_seed()
    {
        GameState first = GameStateFactory.CreateEmpty(8675309UL);
        GameState second = GameStateFactory.CreateEmpty(8675309UL);

        Assert.Equal(first.SchemaVersion, second.SchemaVersion);
        Assert.Equal(first.RootSeed, second.RootSeed);
        Assert.Equal(first.DayIndex, second.DayIndex);
        Assert.Equal(first.SimTime, second.SimTime);
        Assert.Equal(first.Inventory.Quantities, second.Inventory.Quantities);
        Assert.Equal(first.Customers.Customers, second.Customers.Customers);
    }

    [Fact]
    public void Version_one_day_boundary_save_migrates_to_default_reputation()
    {
        GameState state = GameStateFactory.CreateEmpty(7);
        DayCycle.DayBoundarySnapshot legacy = GameStateFactory.CreateDayBoundarySnapshot(state) with
        {
            SaveVersion = 1,
            Reputation = 0,
        };

        GameState restored = GameStateFactory.RestoreDayBoundary(legacy);

        Assert.Equal(50, restored.Progression.Reputation);
    }

    [Theory]
    [InlineData(typeof(GameState), nameof(GameState.DayIndex))]
    [InlineData(typeof(GameState), nameof(GameState.SimTime))]
    [InlineData(typeof(BusinessState), nameof(BusinessState.CashMinorUnits))]
    public void Presentation_code_cannot_directly_set_domain_values(Type type, string propertyName)
    {
        MethodInfo? setter = type.GetProperty(propertyName)?.SetMethod;

        Assert.NotNull(setter);
        Assert.False(setter!.IsPublic);
    }
}
