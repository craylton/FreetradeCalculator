using System.Globalization;
using System.Text;
using FreetradeCalculator.Domain;

namespace FreetradeCalculator.IO;

public static class CsvTradeReader
{
	private static readonly string[] RequiredHeaders =
	[
		"Type",
		"Buy / Sell",
		"Title",
		"Price per Share in Account Currency",
		"Quantity",
		"Timestamp",
	];

	public static IReadOnlyList<Trade> ReadTrades(string path)
	{
		using var reader = new StreamReader(path);
		var headerLine = reader.ReadLine();
		if (headerLine is null)
			throw new ValidationException("CSV appears to be empty.");

		var headers = ParseCsvLine(headerLine).ToArray();
		var headerIndex = BuildHeaderIndex(headers);
		ValidateRequiredHeaders(headerIndex);

		var trades = new List<Trade>();
		var lineNumber = 1;
		while (reader.ReadLine() is { } line)
		{
			lineNumber++;
			if (string.IsNullOrWhiteSpace(line))
				continue;

			var fields = ParseCsvLine(line).ToArray();
			string Get(string header)
			{
				var idx = headerIndex[header];
				return idx < fields.Length ? fields[idx] : "";
			}

			if (!string.Equals(Get("Type"), "ORDER", StringComparison.OrdinalIgnoreCase))
				continue;

			var title = Get("Title").Trim();
			if (string.IsNullOrWhiteSpace(title))
				throw new ValidationException($"Invalid ORDER row: Title is missing (line {lineNumber}).");

			var sideText = Get("Buy / Sell").Trim();
			var side = sideText.ToUpperInvariant() switch
			{
				"BUY" => TradeSide.Buy,
				"SELL" => TradeSide.Sell,
				_ => throw new ValidationException($"Invalid ORDER row: Buy/Sell must be BUY or SELL for '{title}' (line {lineNumber}).")
			};

			var quantity = ParseDecimalInvariant(Get("Quantity"), title, lineNumber, "Quantity");
			if (quantity <= 0)
				throw new ValidationException($"Invalid ORDER row: Quantity must be > 0 for '{title}' (line {lineNumber}).");

			var price = ParseDecimalInvariant(Get("Price per Share in Account Currency"), title, lineNumber, "Price per Share in Account Currency");
			if (price < 0)
				throw new ValidationException($"Invalid ORDER row: Price must be >= 0 for '{title}' (line {lineNumber}).");

			var timestamp = ParseTimestamp(Get("Timestamp"), title, lineNumber);

			trades.Add(new Trade(title, side, quantity, price, timestamp, lineNumber));
		}

		return trades;
	}

	private static Dictionary<string, int> BuildHeaderIndex(string[] headers)
	{
		var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (var i = 0; i < headers.Length; i++)
		{
			var name = headers[i].Trim();
			if (name.Length == 0)
				continue;
			if (!dict.ContainsKey(name))
				dict.Add(name, i);
		}
		return dict;
	}

	private static void ValidateRequiredHeaders(Dictionary<string, int> headerIndex)
	{
		var missing = RequiredHeaders.Where(h => !headerIndex.ContainsKey(h)).ToArray();
		if (missing.Length > 0)
			throw new ValidationException($"CSV is missing required header(s): {string.Join(", ", missing)}");
	}

	private static decimal ParseDecimalInvariant(string text, string title, int lineNumber, string field)
	{
		if (!decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
			throw new ValidationException($"Invalid ORDER row: Could not parse {field} for '{title}' (line {lineNumber}).");
		return value;
	}

	private static DateTimeOffset ParseTimestamp(string text, string title, int lineNumber)
	{
		if (DateTimeOffset.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dto))
			return dto;

		if (DateTimeOffset.TryParse(text.Trim(), CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out dto))
			return dto;

		throw new ValidationException($"Invalid ORDER row: Could not parse Timestamp for '{title}' (line {lineNumber}).");
	}

	private static IEnumerable<string> ParseCsvLine(string line)
	{
		var sb = new StringBuilder();
		var inQuotes = false;

		for (var i = 0; i < line.Length; i++)
		{
			var c = line[i];
			if (c == '"')
			{
				if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
				{
					_ = sb.Append('"');
					i++;
					continue;
				}

				inQuotes = !inQuotes;
				continue;
			}

			if (c == ',' && !inQuotes)
			{
				yield return sb.ToString();
				_ = sb.Clear();
				continue;
			}

			_ = sb.Append(c);
		}

		yield return sb.ToString();
	}
}
