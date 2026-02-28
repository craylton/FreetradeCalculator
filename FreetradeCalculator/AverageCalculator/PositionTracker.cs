using FreetradeCalculator.Domain;

namespace FreetradeCalculator.AverageCalculator;

public sealed class PositionTracker(string title)
{
	private decimal _totalBought;
	private decimal _totalSold;
	private decimal _totalSellProceeds;
	private decimal _totalCostBasisOfSoldShares;

	private decimal _holdingQuantity;
	private decimal _holdingCost;

	public void ProcessBuy(Trade trade)
	{
		_totalBought += trade.Quantity;
		_holdingQuantity += trade.Quantity;
		_holdingCost += trade.Quantity * trade.PricePerShare;
	}

	public void ProcessSell(Trade trade)
	{
		if (_holdingQuantity < trade.Quantity)
			throw new ValidationException($"Oversell detected for '{title}' at {trade.Timestamp:o}.");

		_totalSold += trade.Quantity;
		_totalSellProceeds += trade.Quantity * trade.PricePerShare;

		decimal costBasisForThisSale = trade.Quantity == _holdingQuantity
			? _holdingCost
			: trade.Quantity * (_holdingCost / _holdingQuantity);

		_totalCostBasisOfSoldShares += costBasisForThisSale;
		_holdingQuantity -= trade.Quantity;
		_holdingCost -= costBasisForThisSale;
	}

	public PositionSummary ToSummary() =>
		new(title, _totalBought, _totalSold, _totalSellProceeds, _totalCostBasisOfSoldShares);
}
