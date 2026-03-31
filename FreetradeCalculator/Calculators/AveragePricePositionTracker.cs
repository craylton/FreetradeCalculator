using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Calculators;

public sealed class AveragePricePositionTracker(string title) : IPositionTrackingStrategy
{
	private string _title = title;
	private decimal _totalBought;
	private decimal _totalSold;
	private decimal _totalSellProceeds;
	private decimal _totalCostBasisOfSoldShares;

	private decimal _holdingQuantity;
	private decimal _holdingCost;

	public void ProcessBuy(Trade trade)
	{
		_title = trade.Title;
		_totalBought += trade.Quantity;
		_holdingQuantity += trade.Quantity;
		_holdingCost += trade.Quantity * trade.PricePerShare;
	}

	public void ProcessSell(Trade trade)
	{
		_title = trade.Title;
		if (_holdingQuantity < trade.Quantity)
			throw new ValidationException($"Oversell detected for '{trade.Title}' at {trade.Timestamp:o}.");

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
		new(_title, _totalBought, _totalSold, _totalSellProceeds, _totalCostBasisOfSoldShares);
}
