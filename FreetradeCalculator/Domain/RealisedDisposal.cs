namespace FreetradeCalculator.Domain;

public sealed record RealisedDisposal(
	string Isin,
	string Title,
	DateTimeOffset Timestamp,
	decimal QuantitySold,
	decimal SellProceeds,
	decimal CostBasisOfSoldShares)
{
	public TaxYear TaxYear => TaxYear.From(Timestamp);
	public decimal RealisedProfit => SellProceeds - CostBasisOfSoldShares;
}
