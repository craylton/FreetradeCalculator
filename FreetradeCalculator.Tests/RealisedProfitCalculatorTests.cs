using FreetradeCalculator.Calculators;
using FreetradeCalculator.Domain;
using NSubstitute;

namespace FreetradeCalculator.Tests;

public sealed class RealisedProfitCalculatorTests
{
	private static readonly DateTimeOffset BaseTime = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

	private static Trade Buy(string title, int dayOffset = 0) =>
		new(title, TradeSide.Buy, 1, 1m, BaseTime.AddDays(dayOffset));

	private static Trade Sell(string title, int dayOffset = 0) =>
		new(title, TradeSide.Sell, 1, 1m, BaseTime.AddDays(dayOffset));

	[Fact]
	public void Calculate_WhenTwoTitles_CalculatesEachSeparately()
	{
		var buyA = Buy("AAA");
		var buyB = Buy("BBB");
		var sellA = Sell("AAA");
		Trade[] trades = [ buyA, buyB, sellA ];

		var trackerA = Substitute.For<IPositionTrackingStrategy>();
		trackerA.ToSummary().Returns(new PositionSummary("AAA", 0, 0, 0, 0));

		var trackerB = Substitute.For<IPositionTrackingStrategy>();
		trackerB.ToSummary().Returns(new PositionSummary("BBB", 0, 0, 0, 0));

		var calculator = new RealisedProfitCalculator(title => title switch
		{
			"AAA" => trackerA,
			"BBB" => trackerB,
			_ => throw new Exception()
		});

		var results = calculator.Calculate(trades);

		Assert.Equal(2, results.Count);

		trackerA.Received(1).ProcessBuy(buyA);
		trackerA.Received(1).ProcessSell(sellA);
		trackerA.Received(1).ToSummary();

		trackerB.Received(1).ProcessBuy(buyB);
		trackerB.Received(1).ToSummary();
	}

	[Fact]
	public void Calculate_WhenTradesAreUnordered_ProcessesChronologically()
	{
		var buyLater = Buy("AAA", dayOffset: 1);
		var buyEarlier = Buy("AAA", dayOffset: 0);
		Trade[] trades = [ buyLater, buyEarlier ];

		var trackerMock = Substitute.For<IPositionTrackingStrategy>();

		var calculator = new RealisedProfitCalculator(_ => trackerMock);

		calculator.Calculate(trades);

		Received.InOrder(() =>
		{
			trackerMock.ProcessBuy(buyEarlier);
			trackerMock.ProcessBuy(buyLater);
		});
	}

	[Fact]
	public void Calculate_WhenTradeListIsEmpty_ReturnsEmptyList()
	{
		var calculator = new RealisedProfitCalculator(_ => Substitute.For<IPositionTrackingStrategy>());
		var results = calculator.Calculate([]);
		Assert.Empty(results);
	}

	[Fact]
	public void Calculate_WhenTradeSideIsUnknown_ThrowsValidationException()
	{
		Trade[] trades = [ new Trade("UNKNOWN", (TradeSide)999, 1, 1m, BaseTime) ];
		var calculator = new RealisedProfitCalculator(_ => Substitute.For<IPositionTrackingStrategy>());

		var exception = Assert.Throws<ValidationException>(() => calculator.Calculate(trades));

		Assert.Contains("Unknown trade side", exception.Message);
	}
}
