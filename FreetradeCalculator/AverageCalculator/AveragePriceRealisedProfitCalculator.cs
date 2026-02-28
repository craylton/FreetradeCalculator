using FreetradeCalculator.Domain;
using FreetradeCalculator.Calculators;

namespace FreetradeCalculator.AverageCalculator;

public static class AveragePriceRealisedProfitCalculator
{
    public static IReadOnlyList<PositionSummary> Calculate(IEnumerable<Trade> trades) =>
        RealisedProfitCalculator.Calculate(trades, title => new AveragePricePositionTracker(title));
}
