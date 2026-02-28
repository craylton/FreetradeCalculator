using System.Globalization;
using System.Text;
using FreetradeCalculator.Domain;

namespace FreetradeCalculator.IO;

public static class CsvTradeReader
{
	private const string OrderType = "ORDER";

	private static class Headers
	{
		public const string Type = "Type";
		public const string BuySell = "Buy / Sell";
		public const string Title = "Title";
		public const string PricePerShare = "Price per Share in Account Currency";
		public const string Quantity = "Quantity";
		public const string Timestamp = "Timestamp";
	}

	private static readonly string[] RequiredHeaders =
	[
		Headers.Type,
		Headers.BuySell,
		Headers.Title,
		Headers.PricePerShare,
		Headers.Quantity,
		Headers.Timestamp,
	];

	public static IReadOnlyList<Trade> ReadTrades(string path)
	{
		using var reader = new StreamReader(path);
		return ReadTrades(reader);
	}

	public static IReadOnlyList<Trade> ReadTrades(TextReader reader)
	{
        string headerLine = reader.ReadLine() ?? throw new ValidationException("CSV appears to be empty.");
        string[] headers = [.. ParseCsvLine(headerLine)];
        Dictionary<string, int> headerIndex = BuildHeaderIndex(headers);
		ValidateRequiredHeaders(headerIndex);

		var trades = new List<Trade>();
		while (reader.ReadLine() is { } line)
		{
			if (string.IsNullOrWhiteSpace(line))
				continue;

            string[] fields = [.. ParseCsvLine(line)];
            Trade? trade = ParseTrade(fields, headerIndex);
			if (trade is not null)
				trades.Add(trade);
		}

		return trades;
	}

	private static Trade? ParseTrade(string[] fields, Dictionary<string, int> headerIndex)
	{
        string tradeTypeText = GetField(fields, headerIndex, Headers.Type);
        if (!string.Equals(tradeTypeText, OrderType, StringComparison.OrdinalIgnoreCase))
			return null;

        string title = GetField(fields, headerIndex, Headers.Title);
		if (string.IsNullOrWhiteSpace(title))
			throw new ValidationException($"Invalid ORDER row: Title is missing.");

        string sideText = GetField(fields, headerIndex, Headers.BuySell);
        TradeSide side = ParseSide(sideText, title);

        string quantityText = GetField(fields, headerIndex, Headers.Quantity);
        decimal quantity = ParseDecimalInvariant(quantityText, title, Headers.Quantity);
		if (quantity <= 0)
			throw new ValidationException($"Invalid ORDER row: Quantity must be > 0 for '{title}'.");

        string priceText = GetField(fields, headerIndex, Headers.PricePerShare);
        decimal price = ParseDecimalInvariant(priceText, title, Headers.PricePerShare);
		if (price < 0)
			throw new ValidationException($"Invalid ORDER row: Price must be >= 0 for '{title}'.");

        string timestampText = GetField(fields, headerIndex, Headers.Timestamp);
        DateTimeOffset timestamp = ParseTimestamp(timestampText, title);

		return new Trade(title, side, quantity, price, timestamp);
	}

	private static TradeSide ParseSide(string sideText, string title) =>
		sideText.ToUpperInvariant() switch
		{
			"BUY" => TradeSide.Buy,
			"SELL" => TradeSide.Sell,
			_ => throw new ValidationException($"Invalid ORDER row: Buy/Sell must be BUY or SELL for '{title}'.")
		};

	private static string GetField(string[] fields, Dictionary<string, int> headerIndex, string header)
	{
        int index = headerIndex[header];
		return index < fields.Length ? fields[index].Trim() : string.Empty;
	}

	private static Dictionary<string, int> BuildHeaderIndex(string[] headers)
	{
		var headerIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		for (int i = 0; i < headers.Length; i++)
		{
            string name = headers[i].Trim();
			if (name.Length == 0)
				continue;

			headerIndices.TryAdd(name, i);
		}

		return headerIndices;
	}

	private static void ValidateRequiredHeaders(Dictionary<string, int> headerIndex)
	{
        string[] missing = [.. RequiredHeaders.Where(header => !headerIndex.ContainsKey(header))];
		if (missing.Length > 0)
			throw new ValidationException($"CSV is missing required header(s): {string.Join(", ", missing)}");
	}

	private static decimal ParseDecimalInvariant(string text, string title, string fieldName)
	{
		if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
			throw new ValidationException($"Invalid ORDER row: Could not parse {fieldName} for '{title}'.");
		return value;
	}

	private static DateTimeOffset ParseTimestamp(string text, string title)
	{
		CultureInfo[] cultures = [CultureInfo.InvariantCulture, CultureInfo.CurrentCulture];
		foreach (CultureInfo culture in cultures)
		{
			if (DateTimeOffset.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out var dto))
				return dto;
		}

		throw new ValidationException($"Invalid ORDER row: Could not parse Timestamp for '{title}'.");
	}

	private static IEnumerable<string> ParseCsvLine(string line)
	{
		var sb = new StringBuilder();
		var inQuotes = false;

		for (int i = 0; i < line.Length; i++)
		{
            char c = line[i];
			if (c == '"')
			{
				if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
				{
					sb.Append('"');
					i++;
					continue;
				}

				inQuotes = !inQuotes;
				continue;
			}

			if (c == ',' && !inQuotes)
			{
				yield return sb.ToString();
				sb.Clear();
				continue;
			}

			sb.Append(c);
		}

		yield return sb.ToString();
	}
}
