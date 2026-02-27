using FreetradeCalculator.IO;
using FreetradeCalculator.Output;
using FreetradeCalculator.Services;
using FreetradeCalculator;

try
{
	var inputPath = args.Length > 0 ? args[0] : null;
	if (string.IsNullOrWhiteSpace(inputPath))
	{
		Console.Error.WriteLine("Usage: FreetradeCalculator <path-to-trading-history.csv>");
		Environment.ExitCode = 2;
		return;
	}

	if (!File.Exists(inputPath))		
		throw new ValidationException($"CSV file not found: '{inputPath}'");

	var trades = CsvTradeReader.ReadTrades(inputPath);
	var summaries = FifoRealisedProfitCalculator.Calculate(trades);
	ConsoleRenderer.Render(summaries);
}
catch (ValidationException ex)
{
	Console.Error.WriteLine(ex.Message);
	Environment.ExitCode = 1;
}
