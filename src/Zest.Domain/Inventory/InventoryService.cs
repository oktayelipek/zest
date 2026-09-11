using Zest.Domain.Products;

namespace Zest.Domain.Inventory;

public sealed class InventoryService
{
    private readonly InventoryState _state;
    public InventoryService(InventoryState state) => _state = state ?? throw new ArgumentNullException(nameof(state));
    public void ReceiveLot(string lotId, string ingredientId, decimal quantity, DateOnly? expiresOn = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lotId); ArgumentException.ThrowIfNullOrWhiteSpace(ingredientId);
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        _state.AddLot(new RawLotState(lotId, ingredientId, quantity, expiresOn));
    }
    public decimal Available(string ingredientId)
    {
        decimal onHand = _state.Lots.Values.Where(lot => lot.IngredientId == ingredientId).Sum(lot => lot.RemainingQuantity);
        decimal reserved = _state.Reservations.Values.Where(item => item.Status == ReservationStatus.Active).SelectMany(item => item.Allocations).Where(item => item.IngredientId == ingredientId).Sum(item => item.Quantity);
        return onHand - reserved;
    }
    public bool TryReserve(string orderId, RecipeVersion recipe, int servings = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId); ArgumentNullException.ThrowIfNull(recipe);
        if (servings <= 0) throw new ArgumentOutOfRangeException(nameof(servings));
        if (_state.Reservations.ContainsKey(orderId)) return false;
        Dictionary<string, decimal> requirements = recipe.Components.GroupBy(item => item.IngredientId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Sum(item => item.Quantity) * servings, StringComparer.Ordinal);
        if (requirements.Any(item => Available(item.Key) < item.Value)) return false;
        List<ReservationAllocation> allocations = [];
        foreach ((string ingredientId, decimal required) in requirements.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            decimal remaining = required;
            foreach (RawLotState lot in _state.Lots.Values.Where(lot => lot.IngredientId == ingredientId).OrderBy(lot => lot.ExpiresOn ?? DateOnly.MaxValue).ThenBy(lot => lot.LotId, StringComparer.Ordinal))
            {
                decimal held = _state.Reservations.Values.Where(item => item.Status == ReservationStatus.Active).SelectMany(item => item.Allocations).Where(item => item.LotId == lot.LotId).Sum(item => item.Quantity);
                decimal take = Math.Min(remaining, lot.RemainingQuantity - held);
                if (take > 0) allocations.Add(new(lot.LotId, ingredientId, take));
                remaining -= take; if (remaining == 0) break;
            }
        }
        _state.AddReservation(new(orderId, allocations)); return true;
    }
    public bool Consume(string orderId)
    {
        if (!_state.TryGetReservation(orderId, out InventoryReservationState? reservation) || reservation!.Status != ReservationStatus.Active) return false;
        foreach (ReservationAllocation allocation in reservation.Allocations)
        {
            RawLotState lot = _state.Lots[allocation.LotId]; lot.RemainingQuantity -= allocation.Quantity;
            if (lot.RemainingQuantity < 0) throw new InvalidOperationException("Inventory invariant violated: negative lot quantity.");
        }
        reservation.Status = ReservationStatus.Consumed; return true;
    }
    public bool Release(string orderId)
    {
        if (!_state.TryGetReservation(orderId, out InventoryReservationState? reservation) || reservation!.Status != ReservationStatus.Active) return false;
        reservation.Status = ReservationStatus.Released; return true;
    }

    public bool CanRelease(string orderId) =>
        _state.TryGetReservation(orderId, out InventoryReservationState? reservation)
        && reservation!.Status == ReservationStatus.Active;
}
