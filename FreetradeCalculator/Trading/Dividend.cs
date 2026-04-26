namespace FreetradeCalculator.Trading;

public sealed record Dividend(
	string Isin,
	decimal Amount,
	DateTimeOffset Timestamp);
