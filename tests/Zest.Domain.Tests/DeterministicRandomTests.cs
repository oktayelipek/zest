using Zest.Domain.Randomness;
using Xunit;

namespace Zest.Domain.Tests;

public sealed class DeterministicRandomTests
{
    [Fact]
    public void Same_key_produces_same_sequence()
    {
        DeterministicRandom first = DeterministicRandomFactory.Create(
            42, 3, RngStreamNames.CustomerChoice, "customer-17");
        DeterministicRandom second = DeterministicRandomFactory.Create(
            42, 3, RngStreamNames.CustomerChoice, "customer-17");

        ulong[] firstSequence = Enumerable.Range(0, 16).Select(_ => first.NextUInt64()).ToArray();
        ulong[] secondSequence = Enumerable.Range(0, 16).Select(_ => second.NextUInt64()).ToArray();

        Assert.Equal(firstSequence, secondSequence);
    }

    [Fact]
    public void Cosmetic_draws_do_not_change_customer_choice()
    {
        DeterministicRandom choice = DeterministicRandomFactory.Create(
            42, 3, RngStreamNames.CustomerChoice, "customer-17");
        DeterministicRandom cosmetic = DeterministicRandomFactory.Create(
            42, 3, RngStreamNames.Cosmetic, "customer-17");

        ulong expectedChoice = choice.NextUInt64();
        for (int draw = 0; draw < 1_000; draw++)
        {
            cosmetic.NextUInt64();
        }

        DeterministicRandom replayedChoice = DeterministicRandomFactory.Create(
            42, 3, RngStreamNames.CustomerChoice, "customer-17");
        Assert.Equal(expectedChoice, replayedChoice.NextUInt64());
    }

    [Fact]
    public void Descriptor_exposes_seed_stream_day_and_entity_for_debugging()
    {
        DeterministicRandom random = DeterministicRandomFactory.Create(
            8675309, 12, RngStreamNames.WorldWeather, "park-central");

        string trace = random.Descriptor.ToString();

        Assert.Contains("rootSeed=8675309", trace, StringComparison.Ordinal);
        Assert.Contains("day=12", trace, StringComparison.Ordinal);
        Assert.Contains("stream=world.weather", trace, StringComparison.Ordinal);
        Assert.Contains("entity=park-central", trace, StringComparison.Ordinal);
        Assert.Contains("effectiveSeed=0x", trace, StringComparison.Ordinal);
    }

    [Fact]
    public void Stream_names_produce_independent_sequences()
    {
        ulong weather = DeterministicRandomFactory.Create(42, 3, RngStreamNames.WorldWeather).NextUInt64();
        ulong traffic = DeterministicRandomFactory.Create(42, 3, RngStreamNames.WorldTraffic).NextUInt64();
        ulong spawn = DeterministicRandomFactory.Create(42, 3, RngStreamNames.CustomerSpawn).NextUInt64();

        Assert.Equal(3, new HashSet<ulong> { weather, traffic, spawn }.Count);
    }

    [Fact]
    public void Regression_fixture_matches_the_versioned_algorithm()
    {
        DeterministicRandom random = DeterministicRandomFactory.Create(
            0x0123456789ABCDEFUL,
            7,
            RngStreamNames.OperationsVariation,
            "stand-03");

        ulong[] actual = Enumerable.Range(0, 5).Select(_ => random.NextUInt64()).ToArray();

        Assert.Equal(
            new ulong[]
            {
                8_471_519_279_055_549_776UL,
                16_831_668_622_845_798_249UL,
                11_509_283_368_890_614_726UL,
                1_313_102_519_902_473_554UL,
                7_129_616_166_475_739_138UL,
            },
            actual);
    }
}
