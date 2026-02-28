using System.Globalization;
using CsvHelper.Configuration;
using FreetradeCalculator.Domain;

namespace FreetradeCalculator.CsvReader;

public static class CsvTradeReader
{
    private const string OrderType = "ORDER";

    public static IReadOnlyList<Trade> ReadTrades(string path)
    {
        using var reader = new StreamReader(path);
        return ReadTrades(reader);
    }

    public static IReadOnlyList<Trade> ReadTrades(TextReader reader)
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

        return [.. csv
            .GetRecords<TradeCsvRow>()
            .Where(r => string.Equals(r.Type, OrderType, StringComparison.OrdinalIgnoreCase))
            .Select(ParseOrderRow)];
    }

    private static Trade ParseOrderRow(TradeCsvRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Title))
            throw new ValidationException("Invalid ORDER row: Title is missing.");

        if (row.Quantity is not > 0)
            throw new ValidationException($"Invalid ORDER row: Quantity must be > 0 for '{row.Title}'.");

        if (row.PricePerShare is not >= 0)
            throw new ValidationException($"Invalid ORDER row: Price must be >= 0 for '{row.Title}'.");

        if (row.Timestamp is null)
            throw new ValidationException($"Invalid ORDER row: Could not parse Timestamp for '{row.Title}'.");

        var side = row.BuySell?.ToUpperInvariant() switch
        {
            "BUY" => TradeSide.Buy,
            "SELL" => TradeSide.Sell,
            _ => throw new ValidationException($"Invalid ORDER row: Buy/Sell must be BUY or SELL for '{row.Title}'.")
        };

        return new Trade(
            row.Title,
            side,
            row.Quantity.Value,
            row.PricePerShare.Value,
            row.Timestamp.Value);
    }
}
