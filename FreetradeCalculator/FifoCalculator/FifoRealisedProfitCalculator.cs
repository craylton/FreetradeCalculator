using FreetradeCalculator.Domain;
using FreetradeCalculator.Calculators;

namespace FreetradeCalculator.FifoCalculator;

public static class FifoRealisedProfitCalculator
{
    public static IReadOnlyList<PositionSummary> Calculate(IEnumerable<Trade> trades) =>
        RealisedProfitCalculator.Calculate(trades, title => new FifoPositionTracker(title));
}
