namespace FreetradeCalculator.Tax;

public sealed record RealisedDisposal(
	string Isin,
	DateTimeOffset Timestamp,
	decimal QuantitySold,
	decimal SellProceeds,
	decimal CostBasisOfSoldShares)
{
	public TaxYear TaxYear => TaxYear.From(Timestamp);
	public decimal RealisedProfit => SellProceeds - CostBasisOfSoldShares;
}
