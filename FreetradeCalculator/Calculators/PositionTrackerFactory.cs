using FreetradeCalculator.Calculators.Strategies;

namespace FreetradeCalculator.Calculators;

public sealed class PositionTrackerFactory : IPositionTrackerFactory
{
    public IPositionTrackingStrategy Create(PriceTrackingStrategy strategy, string title) =>
        strategy switch
        {
            PriceTrackingStrategy.Fifo => new FifoPositionTracker(title),
            PriceTrackingStrategy.AveragePrice => new AveragePricePositionTracker(title),
            _ => throw new ValidationException($"Unknown price tracking strategy '{strategy}'.")
        };
}
