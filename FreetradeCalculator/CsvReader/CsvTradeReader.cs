using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
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

		try
		{
			return [.. csv.GetRecords<TradeCsvRow>()
				.Where(r => string.Equals(r.Type, OrderType, StringComparison.OrdinalIgnoreCase))
				.Select(row => 
				{
					if (string.IsNullOrWhiteSpace(row.Title))
						throw new ValidationException("Invalid ORDER row: Title is missing.");

					if (!row.Quantity.HasValue || row.Quantity.Value <= 0)
						throw new ValidationException($"Invalid ORDER row: Quantity must be > 0 for '{row.Title}'.");

					if (!row.PricePerShare.HasValue || row.PricePerShare.Value < 0)
						throw new ValidationException($"Invalid ORDER row: Price must be >= 0 for '{row.Title}'.");

					if (row.BuySell?.ToUpperInvariant() is not ("BUY" or "SELL"))
						throw new ValidationException($"Invalid ORDER row: Buy/Sell must be BUY or SELL for '{row.Title}'.");

					if (!row.Timestamp.HasValue)
						throw new ValidationException($"Invalid ORDER row: Could not parse Timestamp for '{row.Title}'.");

					var side = row.BuySell.Equals("BUY", StringComparison.InvariantCultureIgnoreCase)
						? TradeSide.Buy 
						: TradeSide.Sell;

					return new Trade(row.Title ?? string.Empty, side, row.Quantity.Value, row.PricePerShare.Value, row.Timestamp.Value);
				})];
		}
		catch (Exception ex) when (ex is HeaderValidationException || ex is CsvHelper.MissingFieldException)
		{
			throw new ValidationException($"CSV is missing required header(s): {ex.Message}");
		}
		catch (TypeConverterException ex)
		{
			throw new ValidationException($"Invalid ORDER row: Could not parse value. {ex.Message}");
		}
	}
}
