using System.Globalization;
using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Output;

public static class ConsoleRenderer
{
    private record ColumnDefinition(string Header, Func<PositionSummary, string> GetValue, bool AlignRight = true)
    {
        public Column ToColumn(IReadOnlyList<PositionSummary> summaries)
        {
            string[] values = [.. summaries.Select(GetValue)];
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
        new("Title", s => s.Title, false),
        new("Bought", s => FormatQuantity(s.TotalBought)),
        new("Sold", s => FormatQuantity(s.TotalSold)),
        new("Remaining", s => FormatQuantity(s.RemainingQuantity)),
        new("Realised P&L", s => FormatMoney(s.RealisedProfit)),
        new("Sell Proceeds", s => FormatMoney(s.TotalSellProceeds)),
        new("Cost Basis", s => FormatMoney(s.TotalCostBasisOfSoldShares))
    ];

    public static void Render(IReadOnlyList<PositionSummary> summaries)
    {
        if (summaries.Count == 0)
        {
            Console.WriteLine("No positions found.");
            return;
        }

        Column[] columns = [.. ColumnDefinitions.Select(definition => definition.ToColumn(summaries))];

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

    private static string FormatQuantity(decimal value) => value.ToString("0.########", CultureInfo.InvariantCulture);
    private static string FormatMoney(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
