using FreetradeCalculator.Trading;

namespace FreetradeCalculator.TradingHistory;

public sealed record TradeReadResult(
	IReadOnlyList<Trade> Trades,
	IReadOnlyDictionary<string, string> TitlesByIsin);
