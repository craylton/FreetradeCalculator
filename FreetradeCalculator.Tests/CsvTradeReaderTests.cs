using FreetradeCalculator.CsvReader;
using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Tests;

public sealed class CsvTradeReaderTests
{
	[Fact]
	public void ReadTrades_WhenCsvIsEmpty_ThrowsValidationException()
	{
		var ex = Assert.Throws<ValidationException>(() => ReadTradeDataFromCsv("").Trades);
		Assert.Contains("CSV appears to be empty", ex.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ReadTrades_WhenCsvContainsWhitespaceRow_IgnoresIt()
	{
		var csv = """
			Type,Buy / Sell,Title,ISIN,Price per Share in Account Currency,Quantity,Timestamp
			   
			ORDER,BUY,Some ETF,IE00TEST0001,1.23,2,2026-02-01T00:00:00.000Z
			""";

		var trades = ReadTradeDataFromCsv(csv).Trades;

		var trade = Assert.Single(trades);
		Assert.Equal("IE00TEST0001", trade.Isin);
		Assert.Equal(TradeSide.Buy, trade.Side);
		Assert.Equal(2m, trade.Quantity);
		Assert.Equal(1.23m, trade.PricePerShare);
	}

	[Fact]
	public void ReadTrades_WhenCsvContainsNoOrders_ReturnsEmptyList()
	{
		var csv = """
			Type,Buy / Sell,Title,ISIN,Price per Share in Account Currency,Quantity,Timestamp
			TOP_UP,,,,,,2026-02-01T00:00:00.000Z
			INTEREST_FROM_CASH,,,,,,2026-02-01T00:00:00.000Z
			""";

		var trades = ReadTradeDataFromCsv(csv).Trades;

		Assert.Empty(trades);
	}

	[Fact]
	public void ReadTrades_WhenOrderQuantityIsNegative_ThrowsValidationException()
	{
		var csv = """
			Type,Buy / Sell,Title,ISIN,Price per Share in Account Currency,Quantity,Timestamp
			ORDER,BUY,Some ETF,IE00TEST0001,1.23,-1,2026-02-01T00:00:00.000Z
			""";

		var ex = Assert.Throws<ValidationException>(() => ReadTradeDataFromCsv(csv).Trades);
		Assert.Contains("Quantity must be > 0", ex.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ReadTrades_WhenOrderPriceIsNegative_ThrowsValidationException()
	{
		var csv = """
			Type,Buy / Sell,Title,ISIN,Price per Share in Account Currency,Quantity,Timestamp
			ORDER,BUY,Some ETF,IE00TEST0001,-0.01,1,2026-02-01T00:00:00.000Z
			""";

		var ex = Assert.Throws<ValidationException>(() => ReadTradeDataFromCsv(csv).Trades);
		Assert.Contains("Price must be >= 0", ex.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ReadTrades_WhenCsvIsMissingHeaders_ThrowsValidationException()
	{
		var csv = """
			Type,Buy / Sell,Title,ISIN,Price per Share in Account Currency,Quantity
			ORDER,BUY,Some ETF,IE00TEST0001,1.23,1
			""";

		var ex = Assert.Throws<CsvHelper.MissingFieldException>(() => ReadTradeDataFromCsv(csv).Trades);
	}

	[Fact]
	public void ReadTrades_WhenBuySellIsInvalid_ThrowsValidationException()
	{
		var csv = """
			Type,Buy / Sell,Title,ISIN,Price per Share in Account Currency,Quantity,Timestamp
			ORDER,HOLD,Some ETF,IE00TEST0001,1.23,1,2026-02-01T00:00:00.000Z
			""";

		var ex = Assert.Throws<ValidationException>(() => ReadTradeDataFromCsv(csv).Trades);
		Assert.Contains("Buy/Sell", ex.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ReadTrades_WhenTitleContainsComma_ParsesQuotedCsvCorrectly()
	{
		var csv = """
			Type,Buy / Sell,Title,ISIN,Price per Share in Account Currency,Quantity,Timestamp
			ORDER,BUY,"Some, ETF",IE00TEST0001,1.23,1,2026-02-01T00:00:00.000Z
			""";

		var trades = ReadTradeDataFromCsv(csv).Trades;

		var trade = Assert.Single(trades);
		Assert.Equal("IE00TEST0001", trade.Isin);
        Assert.Equal(TradeSide.Buy, trade.Side);
        Assert.Equal(1m, trade.Quantity);
        Assert.Equal(1.23m, trade.PricePerShare);
    }

	[Fact]
	public void ReadTradeData_WhenTitleChanges_KeepsLatestTitleInLookup()
	{
		var csv = """
			Type,Buy / Sell,Title,ISIN,Price per Share in Account Currency,Quantity,Timestamp
			ORDER,BUY,Asset A,IE00TEST0001,1.23,1,2026-02-01T00:00:00.000Z
			ORDER,SELL,Asset B,IE00TEST0001,1.50,1,2026-02-02T00:00:00.000Z
			""";

		var tradeData = ReadTradeDataFromCsv(csv);

		Assert.Equal("Asset B", tradeData.TitlesByIsin["IE00TEST0001"]);
	}

	[Fact]
	public void ReadTrades_WhenOrderIsMissingIsin_ThrowsValidationException()
	{
		var csv = """
			Type,Buy / Sell,Title,ISIN,Price per Share in Account Currency,Quantity,Timestamp
			ORDER,BUY,Some ETF,,1.23,1,2026-02-01T00:00:00.000Z
			""";

		var ex = Assert.Throws<ValidationException>(() => ReadTradeDataFromCsv(csv).Trades);
		Assert.Contains("ISIN is missing", ex.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ReadTrades_WhenReadingExportWithExtraColumns_ParsesOrdersAndSkipsNonOrders()
	{
		var trades = ReadTradesFromDataFile("activity-feed-export_sample.csv");

		Assert.Equal(2, trades.Count);
		Assert.Equal("IE00BG47J908", trades[0].Isin);
        Assert.Equal(TradeSide.Sell, trades[0].Side);
        Assert.Equal(84m, trades[0].Quantity);
        Assert.Equal(119.5417857m, trades[0].PricePerShare);

		Assert.Equal("IE000716YHJ7", trades[1].Isin);
        Assert.Equal(TradeSide.Sell, trades[1].Side);
        Assert.Equal(870m, trades[1].Quantity);
        Assert.Equal(6.44534483m, trades[1].PricePerShare);
    }

	private static TradeReadResult ReadTradeDataFromCsv(string csv)
	{
		using var reader = new StringReader(csv);
		return CsvTradeReader.ReadTradeData(reader);
	}

	private static IReadOnlyList<Trade> ReadTradesFromDataFile(string fileName)
	{
		var path = Path.Combine(AppContext.BaseDirectory, "Data", fileName);
		return CsvTradeReader.ReadTradeData(path).Trades;
	}
}
