using FreetradeCalculator.Calculators.Strategies;

namespace FreetradeCalculator.Calculators;

public interface IPositionTrackerFactory
{
    IPositionTrackingStrategy Create(PriceTrackingStrategy strategy, string title);
}
