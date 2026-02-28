using FreetradeCalculator.Calculators.Strategies;
using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Tests;

public sealed class FifoPositionTrackerTests
{
	private static readonly DateTimeOffset BaseTime = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

	private static Trade Buy(string title, decimal qty, decimal price, int dayOffset = 0) =>
		new(title, TradeSide.Buy, qty, price, BaseTime.AddDays(dayOffset));

	private static Trade Sell(string title, decimal qty, decimal price, int dayOffset = 0) =>
		new(title, TradeSide.Sell, qty, price, BaseTime.AddDays(dayOffset));

    private static PositionSummary GetSummary(params Trade[] trades)
    {
        var tracker = new FifoPositionTracker("AAA");
        foreach (var trade in trades)
        {
            if (trade.Side == TradeSide.Buy) tracker.ProcessBuy(trade);
            if (trade.Side == TradeSide.Sell) tracker.ProcessSell(trade);
        }
        return tracker.ToSummary();
    }

	[Fact]
	public void Process_WhenBuy10ThenSell5_ReturnsCorrectSummary()
	{
		var summary = GetSummary(
            Buy("AAA", 10, 10m, dayOffset: 0),
            Sell("AAA", 5, 15m, dayOffset: 1));

		Assert.Equal(10m, summary.TotalBought);
		Assert.Equal(5m, summary.TotalSold);
		Assert.Equal(5m, summary.RemainingQuantity);
		Assert.Equal(75m, summary.TotalSellProceeds);
		Assert.Equal(50m, summary.TotalCostBasisOfSoldShares);
		Assert.Equal(25m, summary.RealisedProfit);
	}

	[Fact]
	public void Process_WhenTwoBuysThenSell20_UsesFirstInFirstOut()
	{
		var summary = GetSummary(
			Buy("AAA", 10, 10m, dayOffset: 0),
			Buy("AAA", 15, 12m, dayOffset: 1),
			Sell("AAA", 20, 15m, dayOffset: 2));

		Assert.Equal(25m, summary.TotalBought);
		Assert.Equal(20m, summary.TotalSold);
		Assert.Equal(5m, summary.RemainingQuantity);
		Assert.Equal(300m, summary.TotalSellProceeds);
		Assert.Equal(220m, summary.TotalCostBasisOfSoldShares); // 10*10 + 10*12
		Assert.Equal(80m, summary.RealisedProfit);
	}

	[Fact]
	public void Process_WhenInterleavedBuysAndSells_TracksFifoAcrossAllTrades()
	{
		var summary = GetSummary(
			Buy("AAA", 10, 10m, dayOffset: 0),
			Sell("AAA", 5, 15m, dayOffset: 1),
			Buy("AAA", 15, 12m, dayOffset: 2),
			Sell("AAA", 8, 14m, dayOffset: 3));

		Assert.Equal(25m, summary.TotalBought);
		Assert.Equal(13m, summary.TotalSold);
		Assert.Equal(12m, summary.RemainingQuantity);
		Assert.Equal(187m, summary.TotalSellProceeds);
		Assert.Equal(136m, summary.TotalCostBasisOfSoldShares);
		Assert.Equal(51m, summary.RealisedProfit);
	}

	[Fact]
	public void Process_WhenOnlyBuy_ReturnsZeroRealisedProfit()
	{
		var summary = GetSummary(Buy("AAA", 1, 10m));

		Assert.Equal(1m, summary.TotalBought);
		Assert.Equal(0m, summary.TotalSold);
		Assert.Equal(1m, summary.RemainingQuantity);
		Assert.Equal(0m, summary.RealisedProfit);
	}

	[Fact]
	public void Process_WhenBuyThenSellThenBuyAgain_TracksRemainingCorrectly()
	{
		var summary = GetSummary(
			Buy("AAA", 1, 10m, dayOffset: 0),
			Sell("AAA", 1, 15m, dayOffset: 1),
			Buy("AAA", 1, 12m, dayOffset: 2));

		Assert.Equal(2m, summary.TotalBought);
		Assert.Equal(1m, summary.TotalSold);
		Assert.Equal(1m, summary.RemainingQuantity);
		Assert.Equal(5m, summary.RealisedProfit);
	}

	[Fact]
	public void Process_WhenSoldAtALoss_ReturnsNegativeRealisedProfit()
	{
		var summary = GetSummary(
            Buy("AAA", 10, 15m, dayOffset: 0),
            Sell("AAA", 5, 10m, dayOffset: 1));

		Assert.Equal(-25m, summary.RealisedProfit); // 5*10 - 5*15
	}

	[Fact]
	public void Process_MultipleBuysAndSellsWithDifferentSizes_CalculatesCorrectly()
	{
		var summary = GetSummary(
			Buy("AAA", 10, 10m, dayOffset: 0),
			Buy("AAA", 20, 25m, dayOffset: 1),
			Sell("AAA", 15, 30m, dayOffset: 2),
			Buy("AAA", 10, 15m, dayOffset: 3),
			Sell("AAA", 20, 20m, dayOffset: 4));

		Assert.Equal(40m, summary.TotalBought);
		Assert.Equal(35m, summary.TotalSold);
		Assert.Equal(5m, summary.RemainingQuantity);
		Assert.Equal(850m, summary.TotalSellProceeds);
		Assert.Equal(675m, summary.TotalCostBasisOfSoldShares);
		Assert.Equal(175m, summary.RealisedProfit);
	}

	[Fact]
	public void Process_FractionalShares_CalculatesCorrectly()
	{
		var summary = GetSummary(
			Buy("AAA", 1.5m, 10m, dayOffset: 0),
			Buy("AAA", 2.5m, 20m, dayOffset: 1),
			Sell("AAA", 3m, 25m, dayOffset: 2));

		Assert.Equal(4m, summary.TotalBought);
		Assert.Equal(3m, summary.TotalSold);
		Assert.Equal(1m, summary.RemainingQuantity);
		Assert.Equal(75m, summary.TotalSellProceeds);
		Assert.Equal(45m, summary.TotalCostBasisOfSoldShares);
		Assert.Equal(30m, summary.RealisedProfit);
	}

	[Fact]
	public void Process_WhenSellExceedsBought_ThrowsValidationException()
	{
		Assert.Throws<ValidationException>(() => GetSummary(
            Buy("AAA", 5, 10m, dayOffset: 0), 
            Sell("AAA", 10, 15m, dayOffset: 1)));
	}
}
