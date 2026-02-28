using FreetradeCalculator.Output;
using FreetradeCalculator;
using FreetradeCalculator.Domain;
using FreetradeCalculator.FifoCalculator;
using FreetradeCalculator.CsvReader;
using FreetradeCalculator.AverageCalculator;
using FreetradeCalculator.Calculators;

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
//IReadOnlyList<PositionSummary> summaries = FifoRealisedProfitCalculator.Calculate(trades);
//IReadOnlyList<PositionSummary> summaries = AveragePriceRealisedProfitCalculator.Calculate(trades);
IReadOnlyList<PositionSummary> summaries = RealisedProfitCalculator.Calculate(trades, PriceTrackingStrategy.AveragePrice);
ConsoleRenderer.Render(summaries);