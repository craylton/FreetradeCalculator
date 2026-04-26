using FreetradeCalculator.Tax;
using FreetradeCalculator.Trading;

namespace FreetradeCalculator.Positions;

public interface IPositionTrackingStrategy
{
    void ProcessBuy(Trade trade);
    RealisedDisposal ProcessSell(Trade trade);
    PositionSummary ToSummary();
}
