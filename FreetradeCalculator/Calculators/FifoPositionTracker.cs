using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Calculators;

public sealed class FifoPositionTracker(string title) : IPositionTrackingStrategy
{
	private string _title = title;
    private readonly Queue<BuyLot> _lots = new();
    private decimal _totalBought;
    private decimal _totalSold;
    private decimal _totalSellProceeds;
    private decimal _totalCostBasis;

    public void ProcessBuy(Trade trade)
    {
		_title = trade.Title;
        _lots.Enqueue(new BuyLot(trade.Quantity, trade.PricePerShare));
        _totalBought += trade.Quantity;
    }

	public RealisedDisposal ProcessSell(Trade trade)
    {
		_title = trade.Title;
		decimal sellProceeds = trade.Quantity * trade.PricePerShare;
        _totalSold += trade.Quantity;
		_totalSellProceeds += sellProceeds;

        decimal remainingQuantityToSell = trade.Quantity;
		decimal costBasisForThisSale = 0m;

        while (remainingQuantityToSell > 0)
        {
            if (!_lots.TryPeek(out BuyLot? lot))
				throw new ValidationException($"Oversell detected for '{trade.Title}' at {trade.Timestamp:o}.");

            decimal quantityConsumed = Math.Min(remainingQuantityToSell, lot.QuantityRemaining);

			decimal lotCost = quantityConsumed * lot.PricePerShare;
			_totalCostBasis += lotCost;
			costBasisForThisSale += lotCost;
            remainingQuantityToSell -= quantityConsumed;
            lot.QuantityRemaining -= quantityConsumed;

            if (lot.QuantityRemaining == 0)
                _lots.Dequeue();
        }

		return new RealisedDisposal(trade.Isin, trade.Title, trade.Timestamp, trade.Quantity, sellProceeds, costBasisForThisSale);
    }

    public PositionSummary ToSummary() =>
		new(_title, _totalBought, _totalSold, _totalSellProceeds, _totalCostBasis);

    private sealed class BuyLot(decimal quantityRemaining, decimal pricePerShare)
    {
        public decimal QuantityRemaining { get; set; } = quantityRemaining;
        public decimal PricePerShare { get; } = pricePerShare;
    }
}
