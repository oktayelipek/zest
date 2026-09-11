using System.Collections.ObjectModel;

namespace Zest.Domain;

public sealed class CustomerRegistryState
{
    private readonly Dictionary<Guid, CustomerState> _customers = new();

    public IReadOnlyDictionary<Guid, CustomerState> Customers =>
        new ReadOnlyDictionary<Guid, CustomerState>(_customers);

    internal void Register(CustomerState customer) => _customers.Add(customer.Id, customer);
}

public sealed class CustomerState
{
    internal CustomerState(Guid id, Customers.CustomerNeed need, string segmentId, bool noticed)
    {
        Id = id;
        Need = need;
        SegmentId = segmentId;
        Noticed = noticed;
    }

    public Guid Id { get; }
    public Customers.CustomerNeed Need { get; }
    public string SegmentId { get; }
    public bool Noticed { get; }
}
