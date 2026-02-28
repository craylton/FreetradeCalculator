using FreetradeCalculator.AverageCalculator;
using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Tests;

public sealed class AverageRealisedProfitCalculatorTests
{
	private static readonly DateTimeOffset BaseTime = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

	private static Trade Buy(string title, decimal qty, decimal price, int dayOffset = 0) =>
		new(title, TradeSide.Buy, qty, price, BaseTime.AddDays(dayOffset));

	private static Trade Sell(string title, decimal qty, decimal price, int dayOffset = 0) =>
		new(title, TradeSide.Sell, qty, price, BaseTime.AddDays(dayOffset));

	[Fact]
	public void Calculate_WhenBuy10ThenSell5_ReturnsCorrectSummary()
	{
		Trade[] trades = [Buy("AAA", 10, 10m, dayOffset: 0), Sell("AAA", 5, 15m, dayOffset: 1)];

		var results = AveragePriceRealisedProfitCalculator.Calculate(trades);

		var summary = Assert.Single(results);
		Assert.Equal(10m, summary.TotalBought);
		Assert.Equal(5m, summary.TotalSold);
		Assert.Equal(5m, summary.RemainingQuantity);
		Assert.Equal(75m, summary.TotalSellProceeds);
		Assert.Equal(50m, summary.TotalCostBasisOfSoldShares);
		Assert.Equal(25m, summary.RealisedProfit);
	}

	[Fact]
	public void Calculate_WhenTwoBuysThenSell20_UsesAverageCostBasis()
	{
		Trade[] trades =
		[
			Buy("AAA", 10, 10m, dayOffset: 0),
			Buy("AAA", 15, 12m, dayOffset: 1),
			Sell("AAA", 20, 15m, dayOffset: 2),
		];

		var results = AveragePriceRealisedProfitCalculator.Calculate(trades);

		var summary = Assert.Single(results);
		Assert.Equal(25m, summary.TotalBought);
		Assert.Equal(20m, summary.TotalSold);
		Assert.Equal(5m, summary.RemainingQuantity);
		Assert.Equal(300m, summary.TotalSellProceeds);
		Assert.Equal(224m, summary.TotalCostBasisOfSoldShares); // 20 * ((10*10 + 15*12) / 25)
		Assert.Equal(76m, summary.RealisedProfit);
	}

	[Fact]
	public void Calculate_WhenTwoTitles_CalculatesEachSeparately()
	{
		Trade[] trades =
		[
			Buy("AAA", 10, 10m, dayOffset: 0),
			Buy("BBB", 15, 8m, dayOffset: 1),
			Sell("AAA", 5, 15m, dayOffset: 2),
		];

		var results = AveragePriceRealisedProfitCalculator.Calculate(trades);

		Assert.Equal(2, results.Count);

		var aaa = results.Single(s => s.Title == "AAA");
		Assert.Equal(10m, aaa.TotalBought);
		Assert.Equal(5m, aaa.TotalSold);
		Assert.Equal(5m, aaa.RemainingQuantity);
		Assert.Equal(25m, aaa.RealisedProfit);

		var bbb = results.Single(s => s.Title == "BBB");
		Assert.Equal(15m, bbb.TotalBought);
		Assert.Equal(0m, bbb.TotalSold);
		Assert.Equal(15m, bbb.RemainingQuantity);
		Assert.Equal(0m, bbb.RealisedProfit);
	}

	[Fact]
	public void Calculate_WhenInterleavedBuysAndSells_TracksAverageAcrossAllTrades()
	{
		Trade[] trades =
		[
			Buy("AAA", 10, 10m, dayOffset: 0),
			Sell("AAA", 5, 15m, dayOffset: 1),
			Buy("AAA", 15, 12m, dayOffset: 2),
			Sell("AAA", 8, 14m, dayOffset: 3),
		];

		var results = AveragePriceRealisedProfitCalculator.Calculate(trades);

		var summary = Assert.Single(results);
		Assert.Equal(25m, summary.TotalBought);
		Assert.Equal(13m, summary.TotalSold);
		Assert.Equal(12m, summary.RemainingQuantity);
		Assert.Equal(187m, summary.TotalSellProceeds);
		Assert.Equal(142m, summary.TotalCostBasisOfSoldShares);
		Assert.Equal(45m, summary.RealisedProfit);
	}

	[Fact]
	public void Calculate_WhenOnlyBuy_ReturnsZeroRealisedProfit()
	{
		Trade[] trades = [Buy("AAA", 1, 10m)];

		var results = AveragePriceRealisedProfitCalculator.Calculate(trades);

		var summary = Assert.Single(results);
		Assert.Equal(1m, summary.TotalBought);
		Assert.Equal(0m, summary.TotalSold);
		Assert.Equal(1m, summary.RemainingQuantity);
		Assert.Equal(0m, summary.RealisedProfit);
	}

	[Fact]
	public void Calculate_WhenBuyThenSellThenBuyAgain_TracksRemainingCorrectly()
	{
		Trade[] trades =
		[
			Buy("AAA", 1, 10m, dayOffset: 0),
			Sell("AAA", 1, 15m, dayOffset: 1),
			Buy("AAA", 1, 12m, dayOffset: 2),
		];

		var results = AveragePriceRealisedProfitCalculator.Calculate(trades);

		var summary = Assert.Single(results);
		Assert.Equal(2m, summary.TotalBought);
		Assert.Equal(1m, summary.TotalSold);
		Assert.Equal(1m, summary.RemainingQuantity);
		Assert.Equal(5m, summary.RealisedProfit);
	}

	[Fact]
	public void Calculate_WhenTradesAreUnordered_ProcessesChronologically()
	{
		Trade[] trades =
		[
			Sell("AAA", 5, 15m, dayOffset: 1),
			Buy("AAA", 10, 10m, dayOffset: 0),
		];

		var results = AveragePriceRealisedProfitCalculator.Calculate(trades);

		var summary = Assert.Single(results);
		Assert.Equal(5m, summary.TotalSold);
		Assert.Equal(5m, summary.RemainingQuantity);
		Assert.Equal(25m, summary.RealisedProfit);
	}

	[Fact]
	public void Calculate_WhenSoldAtALoss_ReturnsNegativeRealisedProfit()
	{
		Trade[] trades = [Buy("AAA", 10, 15m, dayOffset: 0), Sell("AAA", 5, 10m, dayOffset: 1)];

		var results = AveragePriceRealisedProfitCalculator.Calculate(trades);

		var summary = Assert.Single(results);
		Assert.Equal(-25m, summary.RealisedProfit);
	}

	[Fact]
	public void Calculate_WhenSellExceedsBought_ThrowsValidationException()
	{
		Trade[] trades = [Buy("AAA", 5, 10m, dayOffset: 0), Sell("AAA", 10, 15m, dayOffset: 1)];

		Assert.Throws<ValidationException>(() => AveragePriceRealisedProfitCalculator.Calculate(trades));
	}

	[Fact]
	public void Calculate_WhenTradeListIsEmpty_ReturnsEmptyList()
	{
		var results = AveragePriceRealisedProfitCalculator.Calculate([]);

		Assert.Empty(results);
	}
}
