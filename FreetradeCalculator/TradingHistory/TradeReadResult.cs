using FreetradeCalculator.Trading;

namespace FreetradeCalculator.TradingHistory;

public sealed record TradeReadResult(
	IReadOnlyList<Trade> Trades,
	IReadOnlyList<Dividend> Dividends,
	IReadOnlyDictionary<string, string> TitlesByIsin);
