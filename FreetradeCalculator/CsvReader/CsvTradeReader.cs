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

    public static TradeReadResult ReadTradeData(TextReader reader)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            ShouldSkipRecord = args => args.Row.Parser.Record?.All(string.IsNullOrWhiteSpace) ?? true
        };

        using var csv = new CsvHelper.CsvReader(reader, config);

        ValidateCsvHeaders(csv);
        List<ParsedTrade> parsedTrades = GetParsedTradesFromCsv(csv);
        List<Trade> trades = GetTradesFromParsedTrades(parsedTrades);
        Dictionary<string, string> titlesByIsin = GetIsinToTitleLookup(parsedTrades);

        return new TradeReadResult(trades, titlesByIsin);
    }

    private static void ValidateCsvHeaders(CsvHelper.CsvReader csv)
    {
        if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null)
            throw new ValidationException("CSV appears to be empty or missing header.");
    }

    private static List<ParsedTrade> GetParsedTradesFromCsv(CsvHelper.CsvReader csv) => [.. csv
        .GetRecords<TradeCsvRow>()
        .Where(row => string.Equals(row.Type, OrderType, StringComparison.OrdinalIgnoreCase))
        .Select(MapOrderRowToTrade)];

    private static List<Trade> GetTradesFromParsedTrades(IEnumerable<ParsedTrade> parsedTrades)
    {
        List<Trade> trades = [];

        foreach (ParsedTrade parsedTrade in parsedTrades)
            trades.Add(parsedTrade.Trade);

        return trades;
    }

    private static Dictionary<string, string> GetIsinToTitleLookup(IEnumerable<ParsedTrade> parsedTrades)
    {
        Dictionary<string, string> titlesByIsin = new(StringComparer.Ordinal);

        foreach (ParsedTrade parsedTrade in parsedTrades)
            titlesByIsin[parsedTrade.Trade.Isin] = parsedTrade.Title;

        return titlesByIsin;
    }

    private static ParsedTrade MapOrderRowToTrade(TradeCsvRow row)
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
