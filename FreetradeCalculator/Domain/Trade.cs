namespace FreetradeCalculator.Domain;

public record Trade(
	string Isin,
	string Title,
	TradeSide Side,
	decimal Quantity,
	decimal PricePerShare,
	DateTimeOffset Timestamp);
