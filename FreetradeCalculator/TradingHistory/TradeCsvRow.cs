using CsvHelper.Configuration.Attributes;

namespace FreetradeCalculator.TradingHistory;

internal class TradeCsvRow
{
    [Name("Type")] 
    public string? Type { get; set; }

    [Name("Buy / Sell")] 
    public string? BuySell { get; set; }

    [Name("Title")] 
    public string? Title { get; set; }

    [Name("ISIN")]
    public string? Isin { get; set; }

    [Name("Price per Share in Account Currency")] 
    public decimal? PricePerShare { get; set; }

    [Name("Quantity")] 
    public decimal? Quantity { get; set; }

    [Name("Timestamp")] 
    public DateTimeOffset? Timestamp { get; set; }

    [Optional]
    [Name("Total Amount in Account Currency")]
    public decimal? TotalAmountInAccountCurrency { get; set; }
}
