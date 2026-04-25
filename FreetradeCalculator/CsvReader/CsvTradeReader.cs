using System.Globalization;
using CsvHelper.Configuration;
using FreetradeCalculator.Domain;

namespace FreetradeCalculator.CsvReader;

public static class CsvTradeReader
{
    private const string OrderType = "ORDER";

	public static TradeReadResult ReadTradeData(string path)
	{
		using var reader = new StreamReader(path);
		return ReadTradeData(reader);
	}

    public static IReadOnlyList<Trade> ReadTrades(string path)
    {
		return ReadTradeData(path).Trades;
    }

	public static TradeReadResult ReadTradeData(TextReader reader)
	{
		var config = new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			HasHeaderRecord = true,
			TrimOptions = TrimOptions.Trim,
			ShouldSkipRecord = args => args.Row.Parser.Record?.All(string.IsNullOrWhiteSpace) ?? true
		};

		using var csv = new CsvHelper.CsvReader(reader, config);

		if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null)
			throw new ValidationException("CSV appears to be empty or missing header.");

		List<Trade> trades = [];
		Dictionary<string, string> titlesByIsin = new(StringComparer.Ordinal);

		foreach (ParsedTrade parsedTrade in csv
			.GetRecords<TradeCsvRow>()
			.Where(r => string.Equals(r.Type, OrderType, StringComparison.OrdinalIgnoreCase))
			.Select(ParseOrderRow))
		{
			trades.Add(parsedTrade.Trade);
			titlesByIsin[parsedTrade.Trade.Isin] = parsedTrade.Title;
		}

		return new TradeReadResult(trades, titlesByIsin);
	}

    public static IReadOnlyList<Trade> ReadTrades(TextReader reader)
    {
		return ReadTradeData(reader).Trades;
    }

	private static ParsedTrade ParseOrderRow(TradeCsvRow row)
    {
		if (string.IsNullOrWhiteSpace(row.Isin))
			throw new ValidationException("Invalid ORDER row: ISIN is missing.");

		string displayTitle = string.IsNullOrWhiteSpace(row.Title)
			? row.Isin
			: row.Title;

        if (row.Quantity is not > 0)
			throw new ValidationException($"Invalid ORDER row: Quantity must be > 0 for '{displayTitle}'.");

        if (row.PricePerShare is not >= 0)
			throw new ValidationException($"Invalid ORDER row: Price must be >= 0 for '{displayTitle}'.");

        if (row.Timestamp is null)
			throw new ValidationException($"Invalid ORDER row: Could not parse Timestamp for '{displayTitle}'.");

        var side = row.BuySell?.ToUpperInvariant() switch
        {
            "BUY" => TradeSide.Buy,
            "SELL" => TradeSide.Sell,
			_ => throw new ValidationException($"Invalid ORDER row: Buy/Sell must be BUY or SELL for '{displayTitle}'.")
        };

		return new ParsedTrade(
			new Trade(
				row.Isin,
				side,
				row.Quantity.Value,
				row.PricePerShare.Value,
				row.Timestamp.Value),
			displayTitle);
    }

	private sealed record ParsedTrade(Trade Trade, string Title);
}
