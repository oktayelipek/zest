using System.Collections.ObjectModel;
using Zest.Domain.Inventory;

namespace Zest.Domain;

public sealed class InventoryState
{
    private readonly Dictionary<string, RawLotState> _lots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, InventoryReservationState> _reservations = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, RawLotState> Lots => new ReadOnlyDictionary<string, RawLotState>(_lots);
    public IReadOnlyDictionary<string, InventoryReservationState> Reservations => new ReadOnlyDictionary<string, InventoryReservationState>(_reservations);
    public IReadOnlyDictionary<string, decimal> Quantities => _lots.Values.GroupBy(lot => lot.IngredientId, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.Sum(lot => lot.RemainingQuantity), StringComparer.Ordinal);

    internal void AddLot(RawLotState lot) => _lots.Add(lot.LotId, lot);
    internal void AddReservation(InventoryReservationState reservation) => _reservations.Add(reservation.OrderId, reservation);
    internal bool TryGetReservation(string orderId, out InventoryReservationState? reservation) => _reservations.TryGetValue(orderId, out reservation);
}
