using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Calculators;

public sealed class AveragePricePositionTracker(string isin) : IPositionTrackingStrategy
{
	private readonly string _isin = isin;
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

	public RealisedDisposal ProcessSell(Trade trade)
	{
		if (_holdingQuantity < trade.Quantity)
			throw new ValidationException($"Oversell detected for ISIN '{trade.Isin}' at {trade.Timestamp:o}.");

		decimal sellProceeds = trade.Quantity * trade.PricePerShare;
		_totalSold += trade.Quantity;
		_totalSellProceeds += sellProceeds;

		decimal costBasisForThisSale = trade.Quantity == _holdingQuantity
			? _holdingCost
			: trade.Quantity * (_holdingCost / _holdingQuantity);

		_totalCostBasisOfSoldShares += costBasisForThisSale;
		_holdingQuantity -= trade.Quantity;
		_holdingCost -= costBasisForThisSale;

		return new RealisedDisposal(
            trade.Isin,
            trade.Timestamp,
            trade.Quantity,
            sellProceeds,
            costBasisForThisSale);
	}

	public PositionSummary ToSummary() =>
		new(_isin, _totalBought, _totalSold, _totalSellProceeds, _totalCostBasisOfSoldShares);
}
