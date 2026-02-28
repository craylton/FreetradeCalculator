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

IReadOnlyList<Trade> trades = CsvTradeReader.ReadTrades(inputPath);

var calculator = new RealisedProfitCalculator(title => new AveragePricePositionTracker(title));
IReadOnlyList<PositionSummary> summaries = calculator.Calculate(trades);

ConsoleRenderer.Render(summaries);

return 0;