namespace Zest.Domain.Customers;

public enum CustomerNeed { None, Low, Medium, High }
public enum CustomerDecision { NotPurchased, Purchased }
public enum LostSaleReason { NoticedNothing, NoNeed, PriceTooHigh, PoorProductFit, OutsideOption, QueueAbandonment, ClosingTime }

public sealed record CustomerInteraction(CustomerState Customer, CustomerDecision Decision, string? RecipeId, LostSaleReason? LostSaleReason, decimal OutsideOptionProbability, decimal SelectedUtility);
