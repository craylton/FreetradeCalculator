namespace FreetradeCalculator.Domain;

public sealed record PositionSummary(
	string Title,
	decimal TotalBought,
	decimal TotalSold,
	decimal RemainingQuantity,
	decimal RealisedProfit,
	decimal TotalSellProceeds,
	decimal TotalCostBasisOfSoldShares,
	DateTimeOffset FirstTradeTimestamp,
	DateTimeOffset LastTradeTimestamp);
