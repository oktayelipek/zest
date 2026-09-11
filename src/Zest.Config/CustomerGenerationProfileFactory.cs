using Zest.Domain.Customers;

namespace Zest.Config;

public static class CustomerGenerationProfileFactory
{
    public static CustomerGenerationProfile Create(ContentConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        Dictionary<string, ProductChoiceProfile> products = config.Recipes.Values.ToDictionary(
            item => item.Id,
            item => new ProductChoiceProfile(item.Id, item.SalePriceMinor),
            StringComparer.Ordinal);
        Dictionary<string, CustomerSegmentProfile> segments = config.Segments.Values.ToDictionary(
            item => item.Id,
            item => new CustomerSegmentProfile(
                item.Id,
                item.ReferencePriceMinor,
                item.PriceSensitivity,
                item.NoticeProbability,
                item.InterestProbability,
                item.OutsideOptionUtility,
                item.TasteWeights),
            StringComparer.Ordinal);
        return new CustomerGenerationProfile(segments, products);
    }
}
