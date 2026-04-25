using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Calculators;

public sealed class RealisedProfitCalculator(Func<string, IPositionTrackingStrategy> strategyFactory)
{
    public IReadOnlyList<TaxYearSummary> Calculate(IEnumerable<Trade> trades)
    {
        IEnumerable<RealisedDisposal> realisedDisposals = trades
            .GroupBy(trade => trade.Isin, StringComparer.Ordinal)
            .SelectMany(CalculateForInstrument);

        return [.. realisedDisposals
            .GroupBy(disposal => disposal.TaxYear)
            .OrderBy(group => group.Key)
            .Select(CreateTaxYearSummary)];
    }

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

    private static TaxYearSummary CreateTaxYearSummary(IGrouping<TaxYear, RealisedDisposal> disposalsForTaxYear)
    {
        IEnumerable<TaxYearPositionSummary> positions = disposalsForTaxYear
            .GroupBy(disposal => disposal.Isin, StringComparer.Ordinal)
            .Select(CreatePositionSummary)
            .OrderBy(summary => summary.Isin, StringComparer.Ordinal);

        return new TaxYearSummary(disposalsForTaxYear.Key, [.. positions]);
    }

    private static TaxYearPositionSummary CreatePositionSummary(IGrouping<string, RealisedDisposal> disposalsForInstrument)
    {
        IEnumerable<RealisedDisposal> orderedDisposals = disposalsForInstrument.OrderBy(disposal => disposal.Timestamp);
        decimal totalSold = orderedDisposals.Sum(disposal => disposal.QuantitySold);
        decimal totalSellProceeds = orderedDisposals.Sum(disposal => disposal.SellProceeds);
        decimal totalCostBasisOfSoldShares = orderedDisposals.Sum(disposal => disposal.CostBasisOfSoldShares);

        return new TaxYearPositionSummary(
            disposalsForInstrument.Key,
            totalSold,
            totalSellProceeds,
            totalCostBasisOfSoldShares);
    }
}
