namespace Zest.Domain.Inventory;

public sealed class RawLotState
{
    internal RawLotState(string lotId, string ingredientId, decimal quantity, DateOnly? expiresOn) { LotId = lotId; IngredientId = ingredientId; ReceivedQuantity = quantity; RemainingQuantity = quantity; ExpiresOn = expiresOn; }
    public string LotId { get; }
    public string IngredientId { get; }
    public decimal ReceivedQuantity { get; }
    public decimal RemainingQuantity { get; internal set; }
    public DateOnly? ExpiresOn { get; }
}
public enum ReservationStatus { Active, Consumed, Released }
public sealed record ReservationAllocation(string LotId, string IngredientId, decimal Quantity);
public sealed class InventoryReservationState
{
    internal InventoryReservationState(string orderId, IReadOnlyList<ReservationAllocation> allocations) { OrderId = orderId; Allocations = Array.AsReadOnly(allocations.ToArray()); }
    public string OrderId { get; }
    public IReadOnlyList<ReservationAllocation> Allocations { get; }
    public ReservationStatus Status { get; internal set; } = ReservationStatus.Active;
}
