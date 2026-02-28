using FreetradeCalculator.Output;
using FreetradeCalculator;
using FreetradeCalculator.Domain;
using FreetradeCalculator.FifoCalculator;
using FreetradeCalculator.CsvReader;

try
{
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
    IReadOnlyList<PositionSummary> summaries = FifoRealisedProfitCalculator.Calculate(trades);
    ConsoleRenderer.Render(summaries);
}
catch (ValidationException ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
