using Zest.Domain.Randomness;
using Zest.Domain.World;

namespace Zest.Domain.Customers;

public sealed class CustomerGenerator
{
    public CustomerInteraction Generate(GameState state, PasserbyOpportunity opportunity, CustomerGenerationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(state); ArgumentNullException.ThrowIfNull(opportunity); ArgumentNullException.ThrowIfNull(profile);
        if (profile.SoftmaxTemperature <= 0) throw new ArgumentOutOfRangeException(nameof(profile));
        if (!profile.Segments.TryGetValue(opportunity.SegmentId, out CustomerSegmentProfile? segment)) throw new KeyNotFoundException($"Unknown customer segment '{opportunity.SegmentId}'.");
        DeterministicRandom random = DeterministicRandomFactory.Create(state, RngStreamNames.CustomerChoice, opportunity.Id);
        double needRoll = random.NextDouble();
        CustomerNeed need = needRoll < 0.15d ? CustomerNeed.None : needRoll < 0.45d ? CustomerNeed.Low : needRoll < 0.85d ? CustomerNeed.Medium : CustomerNeed.High;
        bool noticed = random.NextDouble() < (double)segment.NoticeProbability;
        CustomerState customer = new(GuidFromOpportunity(opportunity.Id), need, segment.Id, noticed);
        state.Customers.Register(customer);
        if (!noticed) return new(customer, CustomerDecision.NotPurchased, null, LostSaleReason.NoticedNothing, 1, 0);
        decimal needFactor = need switch { CustomerNeed.None => 0, CustomerNeed.Low => .6m, CustomerNeed.Medium => 1, CustomerNeed.High => 1.15m, _ => 0 };
        if ((decimal)random.NextDouble() >= segment.InterestProbability * needFactor) return new(customer, CustomerDecision.NotPurchased, null, LostSaleReason.NoNeed, 1, 0);
        List<Choice> choices = [];
        foreach (ProductChoiceProfile product in profile.Products.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            decimal taste = segment.TasteWeights.GetValueOrDefault(product.Id, 0);
            decimal penalty = Math.Max(0, product.PriceMinor - segment.ReferencePriceMinor) / (decimal)segment.ReferencePriceMinor * segment.PriceSensitivity;
            choices.Add(new Choice(product.Id, taste - penalty));
        }
        choices.Add(new Choice(null, segment.OutsideOptionUtility));
        decimal max = choices.Max(item => item.Utility);
        decimal[] weights = choices.Select(item => (decimal)Math.Exp((double)((item.Utility - max) / profile.SoftmaxTemperature))).ToArray();
        decimal total = weights.Sum(), cursor = (decimal)random.NextDouble() * total;
        int selectedIndex = 0;
        for (; selectedIndex < weights.Length - 1; selectedIndex++) { cursor -= weights[selectedIndex]; if (cursor < 0) break; }
        Choice selected = choices[selectedIndex];
        decimal outsideProbability = weights[^1] / total;
        if (selected.RecipeId is null)
        {
            LostSaleReason reason = choices.Any(item => item.RecipeId is not null && item.Utility >= segment.OutsideOptionUtility) ? LostSaleReason.OutsideOption : segment.TasteWeights.Values.Any(value => value > 0) ? LostSaleReason.PriceTooHigh : LostSaleReason.PoorProductFit;
            return new(customer, CustomerDecision.NotPurchased, null, reason, outsideProbability, selected.Utility);
        }
        return new(customer, CustomerDecision.Purchased, selected.RecipeId, null, outsideProbability, selected.Utility);
    }
    private static Guid GuidFromOpportunity(string id) => new(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(id)).AsSpan(0, 16));
    private readonly record struct Choice(string? RecipeId, decimal Utility);
}
