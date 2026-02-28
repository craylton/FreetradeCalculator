using FreetradeCalculator.AverageCalculator;
using FreetradeCalculator.Domain;
using FreetradeCalculator.FifoCalculator;

namespace FreetradeCalculator.Calculators;

internal static class RealisedProfitCalculator
{
    public static IReadOnlyList<PositionSummary> Calculate(
        IEnumerable<Trade> trades,
        PriceTrackingStrategy strategy) =>
        strategy switch
        {
            PriceTrackingStrategy.Fifo => Calculate(trades, title => new FifoPositionTracker(title)),
            PriceTrackingStrategy.AveragePrice => Calculate(trades, title => new AveragePricePositionTracker(title)),
            _ => throw new ValidationException($"Unknown price tracking strategy '{strategy}'.")
        };


    public static IReadOnlyList<PositionSummary> Calculate(
        IEnumerable<Trade> trades,
        Func<string, IPositionTrackingStrategy> trackerFactory) =>
        [.. trades
            .GroupBy(trade => trade.Title, StringComparer.Ordinal)
            .Select(tradeGroup => CalculateForTitle(tradeGroup.Key, tradeGroup, trackerFactory))];

    private static PositionSummary CalculateForTitle(
        string title,
        IEnumerable<Trade> tradesForTitle,
        Func<string, IPositionTrackingStrategy> trackerFactory)
    {
        IPositionTrackingStrategy positionTracker = trackerFactory(title);

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
