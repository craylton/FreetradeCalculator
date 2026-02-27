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

		var rows = summaries
			.Select(s => new
			{
				s.Title,
				Bought = FormatQty(s.TotalBought),
				Sold = FormatQty(s.TotalSold),
				Remaining = FormatQty(s.RemainingQuantity),
				Realised = FormatMoney(s.RealisedProfit),
				Proceeds = FormatMoney(s.TotalSellProceeds),
				CostBasis = FormatMoney(s.TotalCostBasisOfSoldShares),
			})
			.ToArray();

		var headers = new[] { "Title", "Bought", "Sold", "Remaining", "Realised P&L", "Sell Proceeds", "Cost Basis" };
		var widths = new int[headers.Length];
		for (var i = 0; i < headers.Length; i++) widths[i] = headers[i].Length;

		foreach (var r in rows)
		{
			widths[0] = Math.Max(widths[0], r.Title.Length);
			widths[1] = Math.Max(widths[1], r.Bought.Length);
			widths[2] = Math.Max(widths[2], r.Sold.Length);
			widths[3] = Math.Max(widths[3], r.Remaining.Length);
			widths[4] = Math.Max(widths[4], r.Realised.Length);
			widths[5] = Math.Max(widths[5], r.Proceeds.Length);
			widths[6] = Math.Max(widths[6], r.CostBasis.Length);
		}

		WriteRow(headers[0], headers[1], headers[2], headers[3], headers[4], headers[5], headers[6]);
		WriteSeparator();
		foreach (var r in rows)
			WriteRow(r.Title, r.Bought, r.Sold, r.Remaining, r.Realised, r.Proceeds, r.CostBasis);

		return;

		void WriteSeparator() => Console.WriteLine(string.Join("-+-", widths.Select(w => new string('-', w))));
		void WriteRow(string a, string b, string c, string d, string e, string f, string g)
		{
			Console.WriteLine(string.Join(" | ",
				Pad(a, widths[0], left: true),
				Pad(b, widths[1], left: false),
				Pad(c, widths[2], left: false),
				Pad(d, widths[3], left: false),
				Pad(e, widths[4], left: false),
				Pad(f, widths[5], left: false),
				Pad(g, widths[6], left: false)));
		}

		static string Pad(string value, int width, bool left) => left ? value.PadRight(width) : value.PadLeft(width);
	}

	private static string FormatQty(decimal value) => value.ToString("0.########", CultureInfo.InvariantCulture);
	private static string FormatMoney(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
