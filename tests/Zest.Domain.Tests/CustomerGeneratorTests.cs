using Zest.Domain.Customers;
using Zest.Domain.World;
using Xunit;

namespace Zest.Domain.Tests;

public sealed class CustomerGeneratorTests
{
    private static readonly PasserbyOpportunity Opportunity = new(
        "park:20260908:12:0001",
        new CalendarTimeState(new DateOnly(2026, 9, 8), 12),
        "park",
        "student",
        "sunny",
        24);

    [Fact]
    public void Same_opportunity_and_seed_produce_same_customer_flow()
    {
        CustomerGenerationProfile profile = Profile(350, 1, new Dictionary<string, decimal> { ["classic"] = 1 });
        CustomerGenerator generator = new();

        CustomerInteraction first = generator.Generate(GameStateFactory.CreateEmpty(42), Opportunity, profile);
        CustomerInteraction second = generator.Generate(GameStateFactory.CreateEmpty(42), Opportunity, profile);

        Assert.Equal(first.Decision, second.Decision);
        Assert.Equal(first.RecipeId, second.RecipeId);
        Assert.Equal(first.LostSaleReason, second.LostSaleReason);
        Assert.Equal(first.OutsideOptionProbability, second.OutsideOptionProbability);
        Assert.Equal(first.Customer.SegmentId, second.Customer.SegmentId);
        Assert.Equal(first.Customer.Need, second.Customer.Need);
    }

    [Fact]
    public void Price_increase_reduces_aggregate_purchase_rate_smoothly()
    {
        CustomerGenerator generator = new();
        int lowPricePurchases = 0;
        int highPricePurchases = 0;
        for (ulong seed = 0; seed < 1_000; seed++)
        {
            if (generator.Generate(GameStateFactory.CreateEmpty(seed), Opportunity, Profile(350, 1, new Dictionary<string, decimal> { ["classic"] = 1 })).Decision == CustomerDecision.Purchased) lowPricePurchases++;
            if (generator.Generate(GameStateFactory.CreateEmpty(seed), Opportunity, Profile(700, 1, new Dictionary<string, decimal> { ["classic"] = 1 })).Decision == CustomerDecision.Purchased) highPricePurchases++;
        }

        Assert.True(lowPricePurchases > highPricePurchases);
        Assert.True(lowPricePurchases - highPricePurchases < 500);
    }

    [Fact]
    public void Segment_taste_preference_changes_product_choice()
    {
        CustomerGenerationProfile classicProfile = Profile(350, 1, new Dictionary<string, decimal> { ["classic"] = 3, ["berry"] = 0 });
        CustomerGenerationProfile berryProfile = Profile(350, 1, new Dictionary<string, decimal> { ["classic"] = 0, ["berry"] = 3 });
        CustomerGenerator generator = new();
        PasserbyOpportunity opportunity = Opportunity;

        int classicChoices = 0;
        int berryChoices = 0;
        for (ulong seed = 0; seed < 200; seed++)
        {
            CustomerInteraction classic = generator.Generate(GameStateFactory.CreateEmpty(seed), opportunity, classicProfile);
            CustomerInteraction berry = generator.Generate(GameStateFactory.CreateEmpty(seed), opportunity, berryProfile);
            if (classic.RecipeId == "classic") classicChoices++;
            if (berry.RecipeId == "berry") berryChoices++;
        }

        Assert.True(classicChoices > berryChoices / 2);
        Assert.True(berryChoices > classicChoices / 2);
    }

    [Fact]
    public void Every_non_purchase_has_a_reason()
    {
        CustomerGenerationProfile profile = Profile(700, 1, new Dictionary<string, decimal> { ["classic"] = 1 });
        CustomerGenerator generator = new();
        List<CustomerInteraction> interactions = Enumerable.Range(0, 500)
            .Select(seed => generator.Generate(GameStateFactory.CreateEmpty((ulong)seed), Opportunity, profile))
            .ToList();

        Assert.Contains(interactions, item => item.Decision == CustomerDecision.NotPurchased);
        Assert.All(interactions.Where(item => item.Decision == CustomerDecision.NotPurchased), item => Assert.NotNull(item.LostSaleReason));
    }

    private static CustomerGenerationProfile Profile(long price, decimal sensitivity, IReadOnlyDictionary<string, decimal> taste) =>
        new(
            new Dictionary<string, CustomerSegmentProfile>
            {
                ["student"] = new("student", 350, sensitivity, 1, 1, .25m, taste),
            },
            new Dictionary<string, ProductChoiceProfile>
            {
                ["classic"] = new("classic", price),
                ["berry"] = new("berry", price),
            });
}
