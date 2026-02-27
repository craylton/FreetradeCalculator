using System.Globalization;
using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Output;

public static class ConsoleRenderer
{
	public static void Render(IReadOnlyList<PositionSummary> summaries)
	{
		if (summaries.Count == 0)
		{
			Console.WriteLine("No ORDER trades found.");
			return;
		}

		string[] headers = ["Title", "Bought", "Sold", "Remaining", "Realised P&L", "Sell Proceeds", "Cost Basis"];

        string[][] rows = [.. summaries.Select(ToRow)];

        int[] widths = [.. headers.Select((header, i) => rows.Select(row => row[i].Length).Prepend(header.Length).Max())];

		WriteRow(headers, widths);
		WriteSeparator(widths);
		foreach (string[] row in rows)
			WriteRow(row, widths);
	}

	private static string[] ToRow(PositionSummary summary) =>
	[
		summary.Title,
		FormatQuantity(summary.TotalBought),
		FormatQuantity(summary.TotalSold),
		FormatQuantity(summary.RemainingQuantity),
		FormatMoney(summary.RealisedProfit),
		FormatMoney(summary.TotalSellProceeds),
		FormatMoney(summary.TotalCostBasisOfSoldShares),
	];

	private static void WriteSeparator(int[] widths) =>
		Console.WriteLine(string.Join("-+-", widths.Select(width => new string('-', width))));

	private static void WriteRow(string[] columns, int[] widths)
	{
        IEnumerable<string> paddedColumns = columns.Select((column, i) => i == 0 ? column.PadRight(widths[i]) : column.PadLeft(widths[i]));
		Console.WriteLine(string.Join(" | ", paddedColumns));
	}

	private static string FormatQuantity(decimal value) => value.ToString("0.########", CultureInfo.InvariantCulture);
	private static string FormatMoney(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
