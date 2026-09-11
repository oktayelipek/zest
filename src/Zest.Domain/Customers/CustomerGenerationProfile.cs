namespace Zest.Domain.Customers;

public sealed record CustomerGenerationProfile(IReadOnlyDictionary<string, CustomerSegmentProfile> Segments, IReadOnlyDictionary<string, ProductChoiceProfile> Products, decimal SoftmaxTemperature = 1m);
public sealed record CustomerSegmentProfile(string Id, long ReferencePriceMinor, decimal PriceSensitivity, decimal NoticeProbability, decimal InterestProbability, decimal OutsideOptionUtility, IReadOnlyDictionary<string, decimal> TasteWeights);
public sealed record ProductChoiceProfile(string Id, long PriceMinor);
