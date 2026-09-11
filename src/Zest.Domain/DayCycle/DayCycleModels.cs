namespace Zest.Domain.DayCycle;

public enum DayPhase { MorningBrief, Live, Report }

public sealed record MorningBrief(
    int DayIndex,
    string Headline,
    string Guidance);

public sealed record DailyReportSnapshot(
    int DayIndex,
    long RevenueMinor,
    long ProfitMinor,
    long ClosingCashMinor,
    int SalesCount,
    int QueueLossCount,
    int RemainingQueue,
    decimal RemainingStock);

public sealed record DaySessionSnapshot(
    DayPhase Phase,
    int DayIndex,
    long SimTime,
    long PlannedPriceMinor,
    int PlannedBatchSize,
    long CashMinor,
    int QueueCount,
    decimal StockOnHand,
    int SalesCount,
    int QueueLossCount,
    DailyReportSnapshot? Report);

public sealed class DayCycleState
{
    private readonly List<string> _commandHistory = [];

    public DayPhase Phase { get; internal set; } = DayPhase.MorningBrief;
    public MorningBrief Brief { get; internal set; } = new(0, "A fresh day in the park", "Balance price, stock, and patience.");
    public long PlannedPriceMinor { get; internal set; } = 350;
    public int PlannedBatchSize { get; internal set; } = 12;
    public int RemainingOpeningServings { get; internal set; }
    public DailyReportSnapshot? Report { get; internal set; }
    public IReadOnlyList<string> CommandHistory => _commandHistory.AsReadOnly();

    internal void Record(string commandName) => _commandHistory.Add(commandName);
}
