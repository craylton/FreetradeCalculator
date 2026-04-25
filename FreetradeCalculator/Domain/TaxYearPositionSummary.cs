namespace FreetradeCalculator.Domain;

public sealed record TaxYearPositionSummary(
	string Isin,
	decimal TotalSold,
	decimal TotalSellProceeds,
	decimal TotalCostBasisOfSoldShares)
{
	public decimal RealisedProfit => TotalSellProceeds - TotalCostBasisOfSoldShares;
}
