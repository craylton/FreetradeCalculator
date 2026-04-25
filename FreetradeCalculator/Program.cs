using FreetradeCalculator.Output;
using FreetradeCalculator.Domain;
using FreetradeCalculator.CsvReader;
using FreetradeCalculator.Calculators;

if (args is not [string inputPath])
{
    Console.Error.WriteLine("Usage: FreetradeCalculator <path-to-trading-history.csv>");
    return 2;
}

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Error: CSV file not found: '{inputPath}'");
    return 1;
}

TradeReadResult tradeData = CsvTradeReader.ReadTradeData(inputPath);

var calculator = new RealisedProfitCalculator(isin => new AveragePricePositionTracker(isin));
IReadOnlyList<TaxYearSummary> summaries = calculator.Calculate(tradeData.Trades);

ConsoleRenderer.Render(summaries, tradeData.TitlesByIsin);

return 0;