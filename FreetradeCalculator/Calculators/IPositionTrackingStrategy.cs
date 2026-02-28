using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Calculators;

public interface IPositionTrackingStrategy
{
    void ProcessBuy(Trade trade);
    void ProcessSell(Trade trade);
    PositionSummary ToSummary();
}
