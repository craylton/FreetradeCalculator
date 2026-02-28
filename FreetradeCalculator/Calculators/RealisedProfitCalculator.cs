using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Calculators;

public sealed class RealisedProfitCalculator(Func<string, IPositionTrackingStrategy> strategyFactory)
{
    public IReadOnlyList<PositionSummary> Calculate(IEnumerable<Trade> trades)
    {
        return[.. trades
            .GroupBy(trade => trade.Title, StringComparer.Ordinal)
            .Select(tradeGroup => CalculateForTitle(tradeGroup.Key, tradeGroup))];
    }

    private PositionSummary CalculateForTitle(
        string title,
        IEnumerable<Trade> tradesForTitle)
    {
        IPositionTrackingStrategy positionTracker = strategyFactory(title);

        foreach (Trade trade in tradesForTitle.OrderBy(t => t.Timestamp))
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
                    throw new ValidationException($"Unknown trade side '{trade.Side}' for '{title}' at {trade.Timestamp:o}.");
            }
        }

        return positionTracker.ToSummary();
    }
}
