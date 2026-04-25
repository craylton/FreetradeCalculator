namespace FreetradeCalculator.Domain;

public sealed record TaxYearSummary(TaxYear TaxYear, IReadOnlyList<TaxYearPositionSummary> Positions)
{
	public decimal TotalRealisedProfit => Positions.Sum(position => position.RealisedProfit);
	public decimal TotalSellProceeds => Positions.Sum(position => position.TotalSellProceeds);
	public decimal TotalCostBasis => Positions.Sum(position => position.TotalCostBasisOfSoldShares);
}
