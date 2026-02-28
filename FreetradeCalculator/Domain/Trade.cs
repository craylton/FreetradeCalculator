namespace FreetradeCalculator.Domain;

public record Trade(
	string Title,
	TradeSide Side,
	decimal Quantity,
	decimal PricePerShare,
	DateTimeOffset Timestamp);
