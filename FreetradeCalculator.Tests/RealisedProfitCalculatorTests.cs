using FreetradeCalculator.Positions;
using FreetradeCalculator.Tax;
using FreetradeCalculator.Trading;
using NSubstitute;

namespace FreetradeCalculator.Tests;

public sealed class RealisedProfitCalculatorTests
{
	private static readonly DateTimeOffset BaseTime = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
	private static string IsinFor(string title) => $"ISIN-{title}";

	private static Trade Buy(string title, string? isin = null, int dayOffset = 0) =>
		new(isin ?? IsinFor(title), TradeSide.Buy, 1, 1m, BaseTime.AddDays(dayOffset));

	private static Trade Sell(string title, string? isin = null, int dayOffset = 0) =>
		new(isin ?? IsinFor(title), TradeSide.Sell, 1, 1m, BaseTime.AddDays(dayOffset));

	[Fact]
	public void Calculate_WhenTwoTitles_CalculatesEachSeparately()
	{
		var buyA = Buy("AAA");
		var buyB = Buy("BBB");
		var sellA = Sell("AAA");
		Trade[] trades = [ buyA, buyB, sellA ];

		var trackerA = Substitute.For<IPositionTrackingStrategy>();
		trackerA.ProcessSell(sellA).Returns(new RealisedDisposal(sellA.Isin, sellA.Timestamp, sellA.Quantity, sellA.Quantity * sellA.PricePerShare, sellA.Quantity));

		var trackerB = Substitute.For<IPositionTrackingStrategy>();

		var calculator = new RealisedProfitCalculator(isin => isin switch
		{
			"ISIN-AAA" => trackerA,
			"ISIN-BBB" => trackerB,
			_ => throw new Exception()
		});

		var results = calculator.Calculate(trades);

		var taxYear = Assert.Single(results);
		var position = Assert.Single(taxYear.Positions);
		Assert.Equal("ISIN-AAA", position.Isin);

		trackerA.Received(1).ProcessBuy(buyA);
		trackerA.Received(1).ProcessSell(sellA);

		trackerB.Received(1).ProcessBuy(buyB);
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
	public void Calculate_WhenTitleChangesButIsinMatches_AggregatesByIsin()
	{
		Trade[] trades =
		[
			Buy("Asset A", isin: "IE00TEST1234", dayOffset: 0),
			Sell("Asset B", isin: "IE00TEST1234", dayOffset: 1)
		];

		var calculator = new RealisedProfitCalculator(isin => new AveragePricePositionTracker(isin));

		var results = calculator.Calculate(trades);

		var taxYear = Assert.Single(results);
		var summary = Assert.Single(taxYear.Positions);
		Assert.Equal("IE00TEST1234", summary.Isin);
		Assert.Equal(1m, summary.TotalSold);
	}

	[Fact]
	public void Calculate_WhenTitleChangesButIsinMatches_UsesIsinToResolveTracker()
	{
		Trade[] trades =
		[
			Buy("Asset A", isin: "IE00TEST1234", dayOffset: 0),
			Sell("Asset B", isin: "IE00TEST1234", dayOffset: 1)
		];

		var tracker = Substitute.For<IPositionTrackingStrategy>();
		tracker.ProcessSell(trades[1]).Returns(new RealisedDisposal(trades[1].Isin, trades[1].Timestamp, trades[1].Quantity, trades[1].Quantity * trades[1].PricePerShare, trades[1].Quantity));

		var calculator = new RealisedProfitCalculator(isin => isin == "IE00TEST1234" ? tracker : throw new Exception());

		calculator.Calculate(trades);

		tracker.Received(1).ProcessBuy(trades[0]);
		tracker.Received(1).ProcessSell(trades[1]);
	}

	[Fact]
	public void Calculate_WhenAssetIsSoldAcrossTwoTaxYears_GroupsDisposalsSeparately()
	{
		Trade[] trades =
		[
			new("IE00TEST1234", TradeSide.Buy, 100m, 10m, new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero)),
			new("IE00TEST1234", TradeSide.Sell, 50m, 12m, new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero)),
			new("IE00TEST1234", TradeSide.Sell, 50m, 14m, new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero))
		];

		var calculator = new RealisedProfitCalculator(isin => new AveragePricePositionTracker(isin));

		var results = calculator.Calculate(trades);

		Assert.Equal(2, results.Count);

		Assert.Equal(new TaxYear(2024), results[0].TaxYear);
		var firstYearPosition = Assert.Single(results[0].Positions);
		Assert.Equal(50m, firstYearPosition.TotalSold);
		Assert.Equal(600m, firstYearPosition.TotalSellProceeds);
		Assert.Equal(500m, firstYearPosition.TotalCostBasisOfSoldShares);
		Assert.Equal(100m, firstYearPosition.RealisedProfit);

		Assert.Equal(new TaxYear(2025), results[1].TaxYear);
		var secondYearPosition = Assert.Single(results[1].Positions);
		Assert.Equal(50m, secondYearPosition.TotalSold);
		Assert.Equal(700m, secondYearPosition.TotalSellProceeds);
		Assert.Equal(500m, secondYearPosition.TotalCostBasisOfSoldShares);
		Assert.Equal(200m, secondYearPosition.RealisedProfit);
	}

	[Fact]
	public void Calculate_WhenTradeSideIsUnknown_ThrowsValidationException()
	{
		Trade[] trades = [ new Trade("ISIN-UNKNOWN", (TradeSide)999, 1, 1m, BaseTime) ];
		var calculator = new RealisedProfitCalculator(_ => Substitute.For<IPositionTrackingStrategy>());

		var exception = Assert.Throws<ValidationException>(() => calculator.Calculate(trades));

		Assert.Contains("Unknown trade side", exception.Message);
	}
}
