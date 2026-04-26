namespace FreetradeCalculator.Positions;

public sealed record PositionSummary(
	string Isin,
	decimal TotalBought,
	decimal TotalSold,
	decimal TotalSellProceeds,
	decimal TotalCostBasisOfSoldShares)
{
	public decimal RemainingQuantity => TotalBought - TotalSold;
	public decimal RealisedProfit => TotalSellProceeds - TotalCostBasisOfSoldShares;
}
