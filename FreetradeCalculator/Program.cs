using FreetradeCalculator.Output;
using FreetradeCalculator;
using FreetradeCalculator.Domain;
using FreetradeCalculator.CsvReader;
using FreetradeCalculator.Calculators;
using FreetradeCalculator.Calculators.Strategies;

string? inputPath = args.Length > 0 ? args[0] : null;
if (string.IsNullOrWhiteSpace(inputPath))
{
    Console.Error.WriteLine("Usage: FreetradeCalculator <path-to-trading-history.csv>");
    Environment.ExitCode = 2;
    return;
}

if (!File.Exists(inputPath))
    throw new ValidationException($"CSV file not found: '{inputPath}'");

IReadOnlyList<Trade> trades = CsvTradeReader.ReadTrades(inputPath);

var calculator = new RealisedProfitCalculator(new PositionTrackerFactory());
IReadOnlyList<PositionSummary> summaries = calculator.Calculate(
    trades, 
    PriceTrackingStrategy.Fifo);

ConsoleRenderer.Render(summaries);