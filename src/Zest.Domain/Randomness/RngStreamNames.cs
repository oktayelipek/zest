namespace Zest.Domain.Randomness;

/// <summary>Canonical stream names. Never reuse a stream for unrelated decisions.</summary>
public static class RngStreamNames
{
    public const string WorldWeather = "world.weather";
    public const string WorldTraffic = "world.traffic";
    public const string CustomerSpawn = "customer.spawn";
    public const string CustomerChoice = "customer.choice";
    public const string OperationsVariation = "operations.variation";
    public const string QueueAbandonment = "operations.queue-abandonment";
    public const string Cosmetic = "cosmetic";
}
