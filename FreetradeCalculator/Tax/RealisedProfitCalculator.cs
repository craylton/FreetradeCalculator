using FreetradeCalculator.Positions;
using FreetradeCalculator.Trading;

namespace FreetradeCalculator.Tax;

public sealed class RealisedProfitCalculator(Func<string, IPositionTrackingStrategy> strategyFactory)
{
    public IReadOnlyList<TaxYearSummary> Calculate(IEnumerable<Trade> trades, IEnumerable<Dividend> dividends)
    {
        IEnumerable<TaxYearPositionEntry> positionEntries = GetDisposalEntries(trades)
            .Concat(GetDividendEntries(dividends));

        return [.. positionEntries
            .GroupBy(entry => entry.TaxYear)
            .OrderBy(group => group.Key)
            .Select(CreateTaxYearSummary)];
    }

    private IEnumerable<TaxYearPositionEntry> GetDisposalEntries(IEnumerable<Trade> trades)
    {
        IEnumerable<RealisedDisposal> realisedDisposals = trades
            .GroupBy(trade => trade.Isin, StringComparer.Ordinal)
            .SelectMany(CalculateForInstrument);

        return realisedDisposals
			.GroupBy(disposal => disposal.TaxYear)
			.SelectMany(CreateDisposalEntriesForTaxYear);
    }

    private static IEnumerable<TaxYearPositionEntry> GetDividendEntries(IEnumerable<Dividend> dividends) => dividends
        .GroupBy(dividend => (TaxYear.From(dividend.Timestamp), dividend.Isin))
        .Select(CreateDividendEntry);

    private IReadOnlyList<RealisedDisposal> CalculateForInstrument(IGrouping<string, Trade> tradesForInstrument)
    {
        string isin = tradesForInstrument.Key;
        IPositionTrackingStrategy positionTracker = strategyFactory(isin);

        List<RealisedDisposal> realisedDisposals = [];

        foreach (Trade trade in tradesForInstrument.OrderBy(trade => trade.Timestamp))
            ProcessTrade(positionTracker, trade, realisedDisposals);

        return realisedDisposals;
    }

    private static void ProcessTrade(
        IPositionTrackingStrategy positionTracker,
        Trade trade,
        List<RealisedDisposal> realisedDisposals)
    {
        if (trade.Side == TradeSide.Buy)
            positionTracker.ProcessBuy(trade);
        else if (trade.Side == TradeSide.Sell)
            realisedDisposals.Add(positionTracker.ProcessSell(trade));
        else
            throw new ValidationException($"Unknown trade side '{trade.Side}' for ISIN '{trade.Isin}' at {trade.Timestamp:o}.");
    }

    private static TaxYearSummary CreateTaxYearSummary(IGrouping<TaxYear, TaxYearPositionEntry> entriesForTaxYear)
    {
        IEnumerable<TaxYearPositionSummary> positions = entriesForTaxYear
            .GroupBy(entry => entry.Isin, StringComparer.Ordinal)
            .Select(CreatePositionSummary)
            .OrderBy(summary => summary.Isin, StringComparer.Ordinal);

        return new TaxYearSummary(entriesForTaxYear.Key, [.. positions]);
    }

	private static IEnumerable<TaxYearPositionEntry> CreateDisposalEntriesForTaxYear(IGrouping<TaxYear, RealisedDisposal> disposalsForTaxYear) =>
		disposalsForTaxYear
			.GroupBy(disposal => disposal.Isin, StringComparer.Ordinal)
			.Select(disposalsForInstrument => CreateDisposalEntry(disposalsForTaxYear.Key, disposalsForInstrument.Key, disposalsForInstrument));

	private static TaxYearPositionEntry CreateDisposalEntry(
		TaxYear taxYear,
		string isin,
		IEnumerable<RealisedDisposal> disposals)
	{
		IEnumerable<RealisedDisposal> orderedDisposals = disposals.OrderBy(disposal => disposal.Timestamp);
		decimal totalSold = orderedDisposals.Sum(disposal => disposal.QuantitySold);
		decimal totalSellProceeds = orderedDisposals.Sum(disposal => disposal.SellProceeds);
		decimal totalCostBasisOfSoldShares = orderedDisposals.Sum(disposal => disposal.CostBasisOfSoldShares);

		return new TaxYearPositionEntry(
			taxYear,
			isin,
			totalSold,
			totalSellProceeds,
			totalCostBasisOfSoldShares,
			0m);
	}

    private static TaxYearPositionEntry CreateDividendEntry(IGrouping<(TaxYear TaxYear, string Isin), Dividend> dividendsForInstrument)
    {
        decimal totalDividends = dividendsForInstrument.Sum(dividend => dividend.Amount);

        return new TaxYearPositionEntry(
            dividendsForInstrument.Key.TaxYear,
            dividendsForInstrument.Key.Isin,
            0m,
            0m,
            0m,
            totalDividends);
    }

    private static TaxYearPositionSummary CreatePositionSummary(IGrouping<string, TaxYearPositionEntry> entriesForInstrument)
    {
        decimal totalSold = entriesForInstrument.Sum(entry => entry.TotalSold);
        decimal totalSellProceeds = entriesForInstrument.Sum(entry => entry.TotalSellProceeds);
        decimal totalCostBasisOfSoldShares = entriesForInstrument.Sum(entry => entry.TotalCostBasisOfSoldShares);
        decimal totalDividends = entriesForInstrument.Sum(entry => entry.TotalDividends);

        return new TaxYearPositionSummary(
            entriesForInstrument.Key,
            totalSold,
            totalSellProceeds,
            totalCostBasisOfSoldShares,
            totalDividends);
    }

    private sealed record TaxYearPositionEntry(
        TaxYear TaxYear,
        string Isin,
        decimal TotalSold,
        decimal TotalSellProceeds,
        decimal TotalCostBasisOfSoldShares,
        decimal TotalDividends);
}
