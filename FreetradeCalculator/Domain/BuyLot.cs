namespace FreetradeCalculator.Domain;

public sealed record BuyLot(decimal QuantityRemaining, decimal PricePerShare, DateTimeOffset Timestamp) { }
