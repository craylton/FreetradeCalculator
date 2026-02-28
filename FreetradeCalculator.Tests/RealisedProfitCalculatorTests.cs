using FreetradeCalculator.Calculators;
using FreetradeCalculator.Calculators.Strategies;
using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Tests;

public sealed class RealisedProfitCalculatorTests
{
	private static readonly DateTimeOffset BaseTime = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

	private static Trade Buy(string title, int dayOffset = 0) =>
		new(title, TradeSide.Buy, 1, 1m, BaseTime.AddDays(dayOffset));

	private static Trade Sell(string title, int dayOffset = 0) =>
		new(title, TradeSide.Sell, 1, 1m, BaseTime.AddDays(dayOffset));

    private sealed class FakeTracker(string title) : IPositionTrackingStrategy
    {
        public List<Trade> ProcessedTrades { get; } = [];
        public void ProcessBuy(Trade trade) => ProcessedTrades.Add(trade);
        public void ProcessSell(Trade trade) => ProcessedTrades.Add(trade);
        public PositionSummary ToSummary() => new(title, 0, 0, 0, 0);
    }

    private sealed class FakeFactory : IPositionTrackerFactory
    {
        public List<FakeTracker> Trackers { get; } = [];
        public IPositionTrackingStrategy Create(PriceTrackingStrategy strategy, string title)
        {
            var tracker = new FakeTracker(title);
            Trackers.Add(tracker);
            return tracker;
        }
    }

	[Fact]
	public void Calculate_WhenTwoTitles_CalculatesEachSeparately()
	{
		Trade[] trades = [ Buy("AAA"), Buy("BBB"), Sell("AAA") ];
        var factory = new FakeFactory();
        var calculator = new RealisedProfitCalculator(factory);
		
        var results = calculator.Calculate(trades, PriceTrackingStrategy.Fifo);

        Assert.Equal(2, results.Count);
        Assert.Equal(2, factory.Trackers.Count);
        Assert.Contains(factory.Trackers, t => t.ProcessedTrades.Count == 2); // AAA
        Assert.Contains(factory.Trackers, t => t.ProcessedTrades.Count == 1); // BBB
	}

	[Fact]
	public void Calculate_WhenTradesAreUnordered_ProcessesChronologically()
	{
        Trade[] trades = [ Buy("AAA", dayOffset: 1), Buy("AAA", dayOffset: 0) ];
        var factory = new FakeFactory();
        var calculator = new RealisedProfitCalculator(factory);

        calculator.Calculate(trades, PriceTrackingStrategy.Fifo);

        var tracker = Assert.Single(factory.Trackers);
        Assert.Equal(2, tracker.ProcessedTrades.Count);
        Assert.Equal(BaseTime, tracker.ProcessedTrades[0].Timestamp);
        Assert.Equal(BaseTime.AddDays(1), tracker.ProcessedTrades[1].Timestamp);
	}

	[Fact]
	public void Calculate_WhenTradeListIsEmpty_ReturnsEmptyList()
    {
        var calculator = new RealisedProfitCalculator(new FakeFactory());
        var results = calculator.Calculate([], PriceTrackingStrategy.Fifo);
        Assert.Empty(results);
    }
}
