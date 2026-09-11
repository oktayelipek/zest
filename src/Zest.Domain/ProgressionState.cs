namespace Zest.Domain;

public sealed class ProgressionState
{
    private readonly HashSet<string> _unlocks = new(StringComparer.Ordinal);

    public IReadOnlySet<string> Unlocks => _unlocks;
    public int Reputation { get; private set; } = 50;

    public bool Has(string upgradeId) => _unlocks.Contains(upgradeId);
    internal bool Unlock(string upgradeId) => _unlocks.Add(upgradeId);
    internal void Restore(IEnumerable<string> upgradeIds)
    {
        _unlocks.Clear();
        foreach (string id in upgradeIds) _unlocks.Add(id);
    }

    internal void RestoreReputation(int reputation) => Reputation = Math.Clamp(reputation, 0, 100);
    internal void AdjustReputation(int delta) => Reputation = Math.Clamp(Reputation + delta, 0, 100);
}
