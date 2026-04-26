namespace FreetradeCalculator.Tax;

public sealed record TaxYearPositionSummary(
	string Isin,
	decimal TotalSold,
	decimal TotalSellProceeds,
	decimal TotalCostBasisOfSoldShares,
	decimal TotalDividends)
{
	public decimal RealisedProfit => TotalSellProceeds - TotalCostBasisOfSoldShares;
}
