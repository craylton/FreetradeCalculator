using FreetradeCalculator.Domain;

namespace FreetradeCalculator.FifoCalculator;

public static class FifoRealisedProfitCalculator
{
    public static IReadOnlyList<PositionSummary> Calculate(IEnumerable<Trade> trades) => [.. trades
        .GroupBy(trade => trade.Title, StringComparer.Ordinal)
        .Select(tradeGroup => CalculateForTitle(tradeGroup.Key, tradeGroup.OrderBy(t => t.Timestamp)))];

    private static PositionSummary CalculateForTitle(string title, IEnumerable<Trade> tradesForTitle)
    {
        var positionTracker = new PositionTracker(title);

        foreach (Trade trade in tradesForTitle)
        {
            if (trade.Side == TradeSide.Buy)
                positionTracker.ProcessBuy(trade);
            else
                positionTracker.ProcessSell(trade);
        }

        return positionTracker.ToSummary();
    }
}
