using System.Globalization;
using FreetradeCalculator.Tax;

namespace FreetradeCalculator.Reporting;

public static class ConsoleRenderer
{
	private record ColumnDefinition(string Header, Func<TaxYearPositionSummary, IReadOnlyDictionary<string, string>, string> GetValue, bool AlignRight = true)
    {
		public Column ToColumn(IReadOnlyList<TaxYearPositionSummary> summaries, IReadOnlyDictionary<string, string> titlesByIsin)
        {
			string[] values = [.. summaries.Select(summary => GetValue(summary, titlesByIsin))];
            int width = Math.Max(Header.Length, values.Max(value => value.Length));
            return new Column(Header, values, width, AlignRight);
        }
    }

    private record Column(string Header, IReadOnlyList<string> Values, int Width, bool AlignRight)
    {
        public string FormatHeaderCell() => FormatCell(Header);

        public string FormatCell(int row) => FormatCell(Values[row]);

        private string FormatCell(string value) =>
            AlignRight ? value.PadLeft(Width) : value.PadRight(Width);
    }

    private static readonly ColumnDefinition[] ColumnDefinitions =
    [
		new("Title", (s, titlesByIsin) => GetTitle(s.Isin, titlesByIsin), false),
		new("Sold", (s, _) => FormatQuantity(s.TotalSold)),
		new("Realised P&L", (s, _) => FormatMoney(s.RealisedProfit)),
		new("Sell Proceeds", (s, _) => FormatMoney(s.TotalSellProceeds)),
		new("Cost Basis", (s, _) => FormatMoney(s.TotalCostBasisOfSoldShares))
    ];

	public static void Render(IReadOnlyList<TaxYearSummary> summaries, IReadOnlyDictionary<string, string> titlesByIsin)
    {
        if (summaries.Count == 0)
        {
			Console.WriteLine("No realised profits found.");
            return;
        }

		for (int i = 0; i < summaries.Count; i++)
        {
			if (i > 0)
				Console.WriteLine();

			TaxYearSummary summary = summaries[i];
			Console.WriteLine($"Tax Year {summary.TaxYear}");
			RenderPositions(summary.Positions, titlesByIsin);
			RenderTotals(summary.TotalRealisedProfit, summary.TotalSellProceeds, summary.TotalCostBasis);
        }

		if (summaries.Count == 1)
			return;

		decimal totalRealisedProfit = summaries.Sum(summary => summary.TotalRealisedProfit);
		decimal totalSellProceeds = summaries.Sum(summary => summary.TotalSellProceeds);
		decimal totalCostBasis = summaries.Sum(summary => summary.TotalCostBasis);

        Console.WriteLine();
		Console.WriteLine("Overall Totals");
		Console.WriteLine($"  Realised P&L : {FormatMoney(totalRealisedProfit)}");
		Console.WriteLine($"  Sell Proceeds: {FormatMoney(totalSellProceeds)}");
		Console.WriteLine($"  Cost Basis   : {FormatMoney(totalCostBasis)}");
    }

	private static void RenderPositions(IReadOnlyList<TaxYearPositionSummary> summaries, IReadOnlyDictionary<string, string> titlesByIsin)
	{
		TaxYearPositionSummary[] orderedSummaries = [.. summaries
			.OrderBy(summary => GetTitle(summary.Isin, titlesByIsin), StringComparer.Ordinal)
			.ThenBy(summary => summary.Isin, StringComparer.Ordinal)];

		Column[] columns = [.. ColumnDefinitions.Select(definition => definition.ToColumn(orderedSummaries, titlesByIsin))];

		IEnumerable<string> headers = columns.Select(column => column.FormatHeaderCell());
		Console.WriteLine(string.Join(" | ", headers));

		IEnumerable<string> separators = columns.Select(column => new string('-', column.Width));
		Console.WriteLine(string.Join("-+-", separators));

		for (int i = 0; i < summaries.Count; i++)
		{
			IEnumerable<string> cells = columns.Select(column => column.FormatCell(i));
			Console.WriteLine(string.Join(" | ", cells));
		}
	}

	private static void RenderTotals(decimal totalRealisedProfit, decimal totalSellProceeds, decimal totalCostBasis)
	{
		Console.WriteLine();
		Console.WriteLine("Totals");
		Console.WriteLine($"  Realised P&L : {FormatMoney(totalRealisedProfit)}");
		Console.WriteLine($"  Sell Proceeds: {FormatMoney(totalSellProceeds)}");
		Console.WriteLine($"  Cost Basis   : {FormatMoney(totalCostBasis)}");
	}

	private static string GetTitle(string isin, IReadOnlyDictionary<string, string> titlesByIsin) =>
		titlesByIsin.TryGetValue(isin, out string? title) ? title : isin;

    private static string FormatQuantity(decimal value) => value.ToString("0.########", CultureInfo.InvariantCulture);
    private static string FormatMoney(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
