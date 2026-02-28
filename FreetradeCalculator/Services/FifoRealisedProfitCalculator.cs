using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Services;

public static class FifoRealisedProfitCalculator
{
	public static IReadOnlyList<PositionSummary> Calculate(IEnumerable<Trade> trades)
	{
		var ordered = trades
			.OrderBy(t => t.Timestamp)
			.ToArray();

		return ordered
			.GroupBy(t => t.Title, StringComparer.Ordinal)
			.Select(CalculateForTitle)
			.OrderBy(s => s.Title, StringComparer.Ordinal)
			.ToArray();
	}

	private static PositionSummary CalculateForTitle(IGrouping<string, Trade> titleTrades)
	{
		var lots = new LinkedList<BuyLot>();
		decimal totalBought = 0, totalSold = 0, remaining = 0;
		decimal realisedProfit = 0, totalSellProceeds = 0, totalCostBasis = 0;
		DateTimeOffset? first = null, last = null;

		foreach (var trade in titleTrades)
		{
			first ??= trade.Timestamp;
			last = trade.Timestamp;

			if (trade.Side == TradeSide.Buy)
			{
				lots.AddLast(new BuyLot(trade.Quantity, trade.PricePerShare, trade.Timestamp));
				totalBought += trade.Quantity;
				remaining += trade.Quantity;
				continue;
			}

			var remainingToSell = trade.Quantity;
			var sellCostBasis = 0m;
			while (remainingToSell > 0)
			{
				var head = lots.First;
				if (head is null)
					throw new ValidationException($"Oversell detected for '{trade.Title}' at {trade.Timestamp:o}.");

				var lot = head.Value;
				var consumed = Math.Min(remainingToSell, lot.QuantityRemaining);

				sellCostBasis += consumed * lot.PricePerShare;
				remainingToSell -= consumed;
				remaining -= consumed;

				var lotRemaining = lot.QuantityRemaining - consumed;
				if (lotRemaining <= 0)
					lots.RemoveFirst();
				else
					head.Value = lot with { QuantityRemaining = lotRemaining };
			}

			var sellProceeds = trade.Quantity * trade.PricePerShare;
			totalSold += trade.Quantity;
			totalSellProceeds += sellProceeds;
			totalCostBasis += sellCostBasis;
			realisedProfit += sellProceeds - sellCostBasis;
		}

		return new PositionSummary(
			titleTrades.Key,
			totalBought,
			totalSold,
			remaining,
			realisedProfit,
			totalSellProceeds,
			totalCostBasis,
			first ?? default,
			last ?? default);
	}
}
