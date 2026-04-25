namespace FreetradeCalculator.Domain;

public sealed record TaxYearPositionSummary(
	string Isin,
	string Title,
	decimal TotalSold,
	decimal TotalSellProceeds,
	decimal TotalCostBasisOfSoldShares)
{
	public decimal RealisedProfit => TotalSellProceeds - TotalCostBasisOfSoldShares;
}
