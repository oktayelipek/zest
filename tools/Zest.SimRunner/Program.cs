using Zest.Config;
using Zest.Domain;
using Zest.Domain.DayCycle;
using Zest.Domain.Economy;

string configRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "config"));
ContentConfig config = new ConfigLoader().Load(configRoot);
GameState state = ConfigGameStateFactory.CreateEmpty(8675309UL, config);
LiveDayRunner runner = ConfigLiveSessionFactory.Create(state, config, new DateOnly(2026, 9, 10));
DayCommandProcessor commands = new(state, runner);

commands.Execute(new PlanDayCommand(350, 12));
commands.Execute(new StartLiveCommand());
commands.Execute(new AdvanceLiveCommand(10 * 60 * 60));
DaySessionSnapshot result = commands.Execute(new CloseDayCommand());
FinancialSnapshot finance = new EconomyService(state.Business).ProjectDay(state.DayIndex);

Console.WriteLine($"Zest shared live run: schema={state.SchemaVersion}, config={state.ConfigVersion}, seed={state.RootSeed}");
Console.WriteLine($"phase={result.Phase}, sales={result.SalesCount}, queue-losses={result.QueueLossCount}, stock={result.StockOnHand}");
Console.WriteLine($"revenue={finance.Revenue.MinorUnits}, profit={finance.Profit.MinorUnits}, cash={state.Business.CashMinorUnits}");
