namespace FreetradeCalculator.Trading;

public record Trade(
	string Isin,
	TradeSide Side,
	decimal Quantity,
	decimal PricePerShare,
	DateTimeOffset Timestamp);
