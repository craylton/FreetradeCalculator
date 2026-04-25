using FreetradeCalculator.Domain;

namespace FreetradeCalculator.CsvReader;

public sealed record TradeReadResult(
	IReadOnlyList<Trade> Trades,
	IReadOnlyDictionary<string, string> TitlesByIsin);
