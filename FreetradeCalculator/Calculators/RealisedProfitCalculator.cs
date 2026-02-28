using FreetradeCalculator.Calculators.Strategies;
using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Calculators;

public sealed class RealisedProfitCalculator(IPositionTrackerFactory trackerFactory)
{
    public IReadOnlyList<PositionSummary> Calculate(
        IEnumerable<Trade> trades,
        PriceTrackingStrategy strategy)
    {
        return[.. trades
            .GroupBy(trade => trade.Title, StringComparer.Ordinal)
            .Select(tradeGroup => CalculateForTitle(tradeGroup.Key, tradeGroup, strategy))];
    }

    private PositionSummary CalculateForTitle(
        string title,
        IEnumerable<Trade> tradesForTitle,
        PriceTrackingStrategy strategy)
    {
        IPositionTrackingStrategy positionTracker = trackerFactory.Create(strategy, title);

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
