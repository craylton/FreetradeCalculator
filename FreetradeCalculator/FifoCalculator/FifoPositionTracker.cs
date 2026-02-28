using FreetradeCalculator.Calculators;
using FreetradeCalculator.Domain;

namespace FreetradeCalculator.FifoCalculator;

internal sealed class FifoPositionTracker(string title) : IPositionTrackingStrategy
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
