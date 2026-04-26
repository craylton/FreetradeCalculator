using System.Globalization;
using CsvHelper.Configuration;
using FreetradeCalculator.Trading;

namespace FreetradeCalculator.TradingHistory;

public static class CsvTradeReader
{
    private const string OrderType = "ORDER";
    private const string DividendType = "DIVIDEND";

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
        List<ParsedInstrumentActivity> parsedActivities = GetParsedActivitiesFromCsv(csv);
        List<Trade> trades = GetTradesFromParsedActivities(parsedActivities);
        List<Dividend> dividends = GetDividendsFromParsedActivities(parsedActivities);
        Dictionary<string, string> titlesByIsin = GetIsinToTitleLookup(parsedActivities);

        return new TradeReadResult(trades, dividends, titlesByIsin);
    }

    private static void ValidateCsvHeaders(CsvHelper.CsvReader csv)
    {
        if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null)
            throw new ValidationException("CSV appears to be empty or missing header.");
    }

    private static List<ParsedInstrumentActivity> GetParsedActivitiesFromCsv(CsvHelper.CsvReader csv) => [.. csv
        .GetRecords<TradeCsvRow>()
        .Where(IsSupportedInstrumentActivity)
        .Select(MapRowToInstrumentActivity)];

    private static bool IsSupportedInstrumentActivity(TradeCsvRow row) =>
        string.Equals(row.Type, OrderType, StringComparison.OrdinalIgnoreCase)
        || string.Equals(row.Type, DividendType, StringComparison.OrdinalIgnoreCase);

    private static ParsedInstrumentActivity MapRowToInstrumentActivity(TradeCsvRow row) =>
        row.Type?.ToUpperInvariant() switch
        {
            OrderType => MapOrderRowToTrade(row),
            DividendType => MapDividendRowToDividend(row),
            _ => throw new ValidationException($"Unsupported instrument activity type '{row.Type}'.")
        };

    private static List<Trade> GetTradesFromParsedActivities(IEnumerable<ParsedInstrumentActivity> parsedActivities) =>
        [.. parsedActivities.OfType<ParsedTradeActivity>().Select(parsedActivity => parsedActivity.Trade)];

    private static List<Dividend> GetDividendsFromParsedActivities(IEnumerable<ParsedInstrumentActivity> parsedActivities) =>
        [.. parsedActivities.OfType<ParsedDividendActivity>().Select(parsedActivity => parsedActivity.Dividend)];

    private static Dictionary<string, string> GetIsinToTitleLookup(IEnumerable<ParsedInstrumentActivity> parsedActivities)
    {
        Dictionary<string, string> titlesByIsin = new(StringComparer.Ordinal);

        foreach (ParsedInstrumentActivity parsedActivity in parsedActivities)
            titlesByIsin[parsedActivity.Isin] = parsedActivity.Title;

        return titlesByIsin;
    }

    private static ParsedTradeActivity MapOrderRowToTrade(TradeCsvRow row)
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

        return new ParsedTradeActivity(
            new Trade(
                row.Isin,
                side,
                row.Quantity.Value,
                row.PricePerShare.Value,
                row.Timestamp.Value),
            displayTitle);
    }

    private static ParsedDividendActivity MapDividendRowToDividend(TradeCsvRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Isin))
            throw new ValidationException("Invalid DIVIDEND row: ISIN is missing.");

        string displayTitle = string.IsNullOrWhiteSpace(row.Title)
            ? row.Isin
            : row.Title;

        if (row.TotalAmountInAccountCurrency is not > 0)
            throw new ValidationException($"Invalid DIVIDEND row: Amount must be > 0 for '{displayTitle}'.");

        if (row.Timestamp is null)
            throw new ValidationException($"Invalid DIVIDEND row: Could not parse Timestamp for '{displayTitle}'.");

        return new ParsedDividendActivity(
            new Dividend(
                row.Isin,
                row.TotalAmountInAccountCurrency.Value,
                row.Timestamp.Value),
            displayTitle);
    }

    private abstract record ParsedInstrumentActivity(string Isin, string Title);

    private sealed record ParsedTradeActivity(Trade Trade, string Title)
        : ParsedInstrumentActivity(Trade.Isin, Title);

    private sealed record ParsedDividendActivity(Dividend Dividend, string Title)
        : ParsedInstrumentActivity(Dividend.Isin, Title);
}
