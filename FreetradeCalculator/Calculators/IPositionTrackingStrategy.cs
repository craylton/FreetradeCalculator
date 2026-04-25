using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Calculators;

public interface IPositionTrackingStrategy
{
    void ProcessBuy(Trade trade);
    RealisedDisposal ProcessSell(Trade trade);
    PositionSummary ToSummary();
}
