using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Calculators;

public sealed class RealisedProfitCalculator(Func<string, IPositionTrackingStrategy> strategyFactory)
{
	public IReadOnlyList<TaxYearSummary> Calculate(IEnumerable<Trade> trades)
	{
		RealisedDisposal[] realisedDisposals = [.. trades
			.GroupBy(trade => trade.Isin, StringComparer.Ordinal)
			.SelectMany(CalculateForInstrument)];

		return [.. realisedDisposals
			.GroupBy(disposal => disposal.TaxYear)
			.OrderBy(group => group.Key)
			.Select(CreateTaxYearSummary)];
	}

	private IReadOnlyList<RealisedDisposal> CalculateForInstrument(IGrouping<string, Trade> tradesForInstrument)
    {
		Trade[] orderedTrades = [.. tradesForInstrument.OrderBy(t => t.Timestamp)];
		IPositionTrackingStrategy positionTracker = strategyFactory(orderedTrades[0].Isin);
		List<RealisedDisposal> realisedDisposals = [];

		foreach (Trade trade in orderedTrades)
		{
			ProcessTrade(positionTracker, trade, realisedDisposals);
		}

		return realisedDisposals;
    }

	private static void ProcessTrade(
        IPositionTrackingStrategy positionTracker,
        Trade trade,
        ICollection<RealisedDisposal> realisedDisposals)
	{
		switch (trade.Side)
		{
			case TradeSide.Buy:
				positionTracker.ProcessBuy(trade);
				break;

			case TradeSide.Sell:
				realisedDisposals.Add(positionTracker.ProcessSell(trade));
				break;

			default:
				throw new ValidationException($"Unknown trade side '{trade.Side}' for ISIN '{trade.Isin}' at {trade.Timestamp:o}.");
		}
	}

	private static TaxYearSummary CreateTaxYearSummary(IGrouping<TaxYear, RealisedDisposal> disposalsForTaxYear)
	{
		TaxYearPositionSummary[] positions = [.. disposalsForTaxYear
			.GroupBy(disposal => disposal.Isin, StringComparer.Ordinal)
			.Select(CreatePositionSummary)
			.OrderBy(summary => summary.Isin, StringComparer.Ordinal)];

		return new TaxYearSummary(disposalsForTaxYear.Key, positions);
	}

	private static TaxYearPositionSummary CreatePositionSummary(IGrouping<string, RealisedDisposal> disposalsForInstrument)
	{
		RealisedDisposal[] orderedDisposals = [.. disposalsForInstrument.OrderBy(disposal => disposal.Timestamp)];

		return new TaxYearPositionSummary(
			disposalsForInstrument.Key,
			orderedDisposals.Sum(disposal => disposal.QuantitySold),
			orderedDisposals.Sum(disposal => disposal.SellProceeds),
			orderedDisposals.Sum(disposal => disposal.CostBasisOfSoldShares));
	}
}
