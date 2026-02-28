using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Services;

public static class FifoRealisedProfitCalculator
{
    public static IReadOnlyList<PositionSummary> Calculate(IEnumerable<Trade> trades) => [.. trades
        .GroupBy(trade => trade.Title, StringComparer.Ordinal)
        .Select(tradeGroup => CalculateForTitle(tradeGroup.Key, tradeGroup.OrderBy(t => t.Timestamp)))];

    private static PositionSummary CalculateForTitle(string title, IEnumerable<Trade> tradesForTitle)
    {
        var tracker = new PositionTracker(title);

        foreach (Trade trade in tradesForTitle)
        {
            if (trade.Side == TradeSide.Buy)
                tracker.ProcessBuy(trade);
            else
                tracker.ProcessSell(trade);
        }

        return tracker.ToSummary();
    }
}

file sealed class PositionTracker(string title)
{
    private readonly Queue<BuyLot> _lots = new();
    private decimal _totalBought;
    private decimal _totalSold;
    private decimal _totalSellProceeds;
    private decimal _totalCostBasis;

    public void ProcessBuy(Trade trade)
    {
        _lots.Enqueue(new BuyLot(trade.Quantity, trade.PricePerShare));
        _totalBought += trade.Quantity;
    }

    public void ProcessSell(Trade trade)
    {
        _totalSold += trade.Quantity;
        _totalSellProceeds += trade.Quantity * trade.PricePerShare;

        decimal remainingQuantityToSell = trade.Quantity;

        while (remainingQuantityToSell > 0)
        {
            if (!_lots.TryPeek(out BuyLot? lot))
                throw new ValidationException($"Oversell detected for '{title}' at {trade.Timestamp:o}.");

            decimal quantityConsumed = Math.Min(remainingQuantityToSell, lot.QuantityRemaining);

            _totalCostBasis += quantityConsumed * lot.PricePerShare;
            remainingQuantityToSell -= quantityConsumed;
            lot.QuantityRemaining -= quantityConsumed;

            if (lot.QuantityRemaining == 0)
                _lots.Dequeue();
        }
    }

    public PositionSummary ToSummary() =>
        new(title, _totalBought, _totalSold, _totalSellProceeds, _totalCostBasis);

    private sealed class BuyLot(decimal quantityRemaining, decimal pricePerShare)
    {
        public decimal QuantityRemaining { get; set; } = quantityRemaining;
        public decimal PricePerShare { get; } = pricePerShare;
    }
}
