using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Calculators;

public sealed class RealisedProfitCalculator(Func<string, IPositionTrackingStrategy> strategyFactory)
{
    public IReadOnlyList<PositionSummary> Calculate(IEnumerable<Trade> trades)
    {
        return[.. trades
            .GroupBy(trade => trade.Isin, StringComparer.Ordinal)
            .Select(CalculateForInstrument)];
    }

    private PositionSummary CalculateForInstrument(IGrouping<string, Trade> tradesForInstrument)
    {
		Trade[] orderedTrades = [.. tradesForInstrument.OrderBy(t => t.Timestamp)];
		IPositionTrackingStrategy positionTracker = strategyFactory(orderedTrades[0].Title);

		foreach (Trade trade in orderedTrades)
        {
            switch (trade.Side)
            {
                case TradeSide.Buy:
                    positionTracker.ProcessBuy(trade);
                    break;

                case TradeSide.Sell:
                    positionTracker.ProcessSell(trade);
                    break;

                default:
					throw new ValidationException($"Unknown trade side '{trade.Side}' for '{trade.Title}' at {trade.Timestamp:o}.");
            }
        }

        return positionTracker.ToSummary();
    }
}
