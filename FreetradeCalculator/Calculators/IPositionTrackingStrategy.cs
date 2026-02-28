using FreetradeCalculator.Domain;

namespace FreetradeCalculator.Calculators;

internal interface IPositionTrackingStrategy
{
    void ProcessBuy(Trade trade);
    void ProcessSell(Trade trade);
    PositionSummary ToSummary();
}
