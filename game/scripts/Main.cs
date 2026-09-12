using Godot;
using Zest.Config;
using Zest.Persistence;
using Zest.Domain;
using Zest.Domain.DayCycle;
using Zest.Domain.Diagnostics;
using Zest.Domain.Operations;
using Zest.Domain.Simulation;

namespace Zest.Game;

public partial class Main : Control
{
    private enum LiveDestination { None, Stand, Customers, Notebook, Map }

    [Export] public bool StartInLiveStudy { get; set; }

    private static readonly Color Ink = ZestStyle.Palette.Ink;
    private static readonly Color Paper = ZestStyle.Palette.Paper;
    private static readonly Color Cream = ZestStyle.Palette.Cream;
    private static readonly Color Leaf = ZestStyle.Palette.Leaf;
    private static readonly Color Citrus = ZestStyle.Palette.ZestYellow;
    private static readonly Color Rust = ZestStyle.Palette.Rust;
    private static readonly Color Night = ZestStyle.Palette.Charcoal;

    private GameState _state = null!;
    private DayCommandProcessor _commands = null!;
    private LiveDayRunner _runner = null!;
    private ContentConfig _config = null!;
    private readonly DayBoundarySaveService _saves = new();
    private readonly UserPreferenceService _preferencesService = new();
    private UserPreferences _preferences = UserPreferences.Default;
    private UiSoundFeedback _sounds = null!;
    private SimulationClock _clock = null!;
    private OperationalDiagnosticService _diagnostics = null!;
    private MarginContainer _margin = null!;
    private HBoxContainer _masthead = null!;
    private HSeparator _rule = null!;
    private VBoxContainer _page = null!;
    private Label _phaseLabel = null!;
    private SpinBox _price = null!;
    private SpinBox _batch = null!;

    private ParkWorldView _parkWorld = null!;
    private Label _dayTime = null!;
    private Label _weather = null!;
    private Label _cashDelta = null!;
    private Label _reputation = null!;
    private Label _contextTitle = null!;
    private VBoxContainer _contextBody = null!;
    private PanelContainer _contextPanel = null!;
    private PanelContainer _managementPanel = null!;
    private Label _managementTitle = null!;
    private RichTextLabel _managementBody = null!;
    private Button _managementCloseButton = null!;
    private LiveDestination _managementReturnDestination = LiveDestination.None;
    private LiveDestination _currentContext = LiveDestination.None;
    private int _currentCustomerIndex;
    private Label _toast = null!;
    private ProgressBar _dayProgress = null!;
    private Label _speedLabel = null!;
    private PanelContainer _timePanel = null!;
    private Button _endDayButton = null!;
    private readonly Dictionary<SimulationSpeed, Button> _speedButtons = [];
    private readonly Dictionary<LiveDestination, Button> _hotbarButtons = [];
    private SimulationSpeed _resumeSpeed = SimulationSpeed.Normal;
    private bool _closingHourActive;
    private bool _closeScheduled;
    private readonly HashSet<Guid> _observedDepartures = [];
    private double _refreshAccumulator;
    private int _observedSaleLedgerCount;
    private int _observedLossCount;
    private Control _juiceLayer = null!;
    private static readonly string[] RegularNames = ["MARCUS", "LENA", "OMAR", "PRIYA", "JUNE", "SAM"];
    private readonly Dictionary<string, int> _regularVisits = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, string> _customerRegularName = [];
    private readonly HashSet<string> _greetedThisDay = new(StringComparer.Ordinal);
    private HashSet<string> _unlocksAtDayStart = new(StringComparer.Ordinal);

    public override void _Ready()
    {
        string configRoot = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "config"));
        _config = new ConfigLoader().Load(configRoot);
        _preferences = _preferencesService.Load(PreferencesPath);
        _sounds = new UiSoundFeedback { Name = "UiSoundFeedback" };
        AddChild(_sounds);
        ApplyPreferences();
        _state = ConfigGameStateFactory.CreateEmpty(8675309, _config);
        _runner = ConfigLiveSessionFactory.Create(_state, _config, new DateOnly(2026, 9, 10));
        _commands = new DayCommandProcessor(_state, _runner);
        _clock = new SimulationClock(_state);
        _diagnostics = new OperationalDiagnosticService();
        BuildShell();
        SetProcess(false);
        if (StartInLiveStudy)
        {
            _commands.Execute(new PlanDayCommand(350, 12));
            _commands.Execute(new StartLiveCommand());
            ShowLive();
        }
        else
        {
            ShowMorningBrief();
        }
    }

    public override void _Process(double delta)
    {
        if (_state.DayCycle.Phase != DayPhase.Live) return;
        int steps = _clock.Advance(TimeSpan.FromSeconds(delta), (_, _) => _runner.AdvanceTo(_state.SimTime));
        if (steps == 0) return;
        _refreshAccumulator += delta;
        if (_refreshAccumulator < .1) return;
        _refreshAccumulator = 0;
        RefreshLive();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_state.DayCycle.Phase != DayPhase.Live || @event is not InputEventKey { Pressed: true, Echo: false } key)
            return;

        switch (key.Keycode)
        {
            case Key.Key1: ShowStandContext(); break;
            case Key.Key2: ShowCustomerContext(0); break;
            case Key.N: ShowManagement(LiveDestination.Notebook); break;
            case Key.M: ShowManagement(LiveDestination.Map); break;
            case Key.Space: TogglePause(); break;
            case Key.Escape: ReturnToWorld(); break;
            default: return;
        }

        GetViewport().SetInputAsHandled();
    }

    private void BuildShell()
    {
        TextureFilter = TextureFilterEnum.Linear;
        Theme = ZestUiSkin.CreateTheme();
        ColorRect background = new() { Color = Paper, MouseFilter = MouseFilterEnum.Ignore };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        _margin = new MarginContainer();
        _margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_margin);
        VBoxContainer shell = new();
        shell.AddThemeConstantOverride("separation", ZestStyle.Space.Lg);
        _margin.AddChild(shell);

        _masthead = new HBoxContainer();
        _masthead.AddChild(LabelText("ZEST", ZestStyle.Type.Brand, Ink));
        _masthead.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        _phaseLabel = LabelText("MORNING BRIEF", 13, Cream);
        _phaseLabel.AddThemeStyleboxOverride("normal", Box(Night, ZestStyle.Radius.Pill, 8, 1, Night));
        _masthead.AddChild(_phaseLabel);
        shell.AddChild(_masthead);

        _rule = new HSeparator();
        _rule.AddThemeColorOverride("separator", ZestStyle.Palette.Border);
        shell.AddChild(_rule);
        _page = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _page.AddThemeConstantOverride("separation", 16);
        shell.AddChild(_page);
        UseEditorialChrome();
    }

    private void UseEditorialChrome()
    {
        _masthead.Visible = true;
        _rule.Visible = true;
        SetMargins(ZestStyle.Space.Page, ZestStyle.Space.Page, 26, 28);
    }

    private void UseWorldFirstChrome()
    {
        _masthead.Visible = false;
        _rule.Visible = false;
        SetMargins(0, 0, 0, 0);
    }

    private void SetMargins(int left, int right, int top, int bottom)
    {
        _margin.AddThemeConstantOverride("margin_left", left);
        _margin.AddThemeConstantOverride("margin_right", right);
        _margin.AddThemeConstantOverride("margin_top", top);
        _margin.AddThemeConstantOverride("margin_bottom", bottom);
    }

    private void ShowMorningBrief()
    {
        SetProcess(false);
        ClearPage();
        UseEditorialChrome();
        _phaseLabel.Text = "MORNING BRIEF";
        _page.AddChild(LabelText("A fresh day in the park", ZestStyle.Type.Display, Ink));
        _page.AddChild(LabelText("Plan simply. Watch closely. Learn from what the day gives back.", ZestStyle.Type.Body, ZestStyle.Palette.MutedInk));

        if (_state.DayIndex == 0 && _state.Business.Ledger.Count == 0)
        {
            _page.AddChild(Card("NEW HERE?",
                "1  ·  Set the Classic price and opening batch, then open the stand.\n" +
                "2  ·  Guests appear one by one — the badge above each shows what they want.\n" +
                "3  ·  When the day closes, read the report. It tells you what to change tomorrow."));
        }

        HBoxContainer columns = new();
        columns.AddThemeConstantOverride("separation", ZestStyle.Space.Lg);
        _page.AddChild(columns);
        VBoxContainer editorial = Card("TODAY'S NOTE", BuildTodaysNote());
        editorial.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        columns.AddChild(editorial);
        VBoxContainer planning = Card("PLAN THE STAND", "Set the promise you want operations to keep.");
        planning.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        columns.AddChild(planning);

        GridContainer form = new() { Columns = 2 };
        form.AddThemeConstantOverride("h_separation", 14);
        form.AddThemeConstantOverride("v_separation", 10);
        planning.AddChild(form);
        form.AddChild(LabelText("Classic price", ZestStyle.Type.Action, Ink));
        _price = new SpinBox { MinValue = 1, MaxValue = 20, Step = .25, Value = 3.5, Suffix = "  USD" };
        form.AddChild(_price);
        form.AddChild(LabelText("Opening batch", ZestStyle.Type.Action, Ink));
        _batch = new SpinBox { MinValue = 1, MaxValue = 100, Step = 1, Value = 12 };
        form.AddChild(_batch);
        planning.AddChild(LabelText("OPERATIONAL PLAN", ZestStyle.Type.Label, Rust));
        Button start = ActionButton("Open the stand  →", Citrus, Ink);
        start.Pressed += StartDay;
        planning.AddChild(start);
        HBoxContainer saves = new();
        saves.AddThemeConstantOverride("separation", 8);
        Button save = CompactButton("Save morning", Paper, Ink);
        save.Pressed += SaveMorning;
        saves.AddChild(save);
        Button load = CompactButton("Load saved run", Paper, Ink);
        load.Disabled = !File.Exists(SavePath);
        load.Pressed += LoadMorning;
        saves.AddChild(load);
        planning.AddChild(saves);

        VBoxContainer accessibility = Card("ACCESSIBILITY", "Choose a larger interface or reduce decorative motion. These choices are saved separately from a game run.");
        accessibility.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        columns.AddChild(accessibility);
        Button textSize = CompactButton(_preferences.TextScale > 1f ? "Text size: large" : "Text size: standard", Paper, Ink);
        textSize.Pressed += () => SetTextScale(_preferences.TextScale > 1f ? 1f : 1.2f);
        accessibility.AddChild(textSize);
        Button motion = CompactButton(_preferences.ReducedMotion ? "Motion: reduced" : "Motion: full", Paper, Ink);
        motion.Pressed += () => SetReducedMotion(!_preferences.ReducedMotion);
        accessibility.AddChild(motion);
        string soundLabel = _preferences.SoundLevel switch { >= 2 => "Sound: full", 1 => "Sound: soft", _ => "Sound: off" };
        Button sound = CompactButton(soundLabel, Paper, Ink);
        sound.Pressed += () => CycleSoundLevel();
        accessibility.AddChild(sound);
    }

    private void SetTextScale(float scale)
    {
        _preferences = _preferences with { TextScale = scale };
        SavePreferences();
        ApplyPreferences();
        ShowMorningBrief();
    }

    private void SetReducedMotion(bool enabled)
    {
        _preferences = _preferences with { ReducedMotion = enabled };
        SavePreferences();
        ApplyPreferences();
        ShowMorningBrief();
    }

    private void SetSoundEnabled(bool enabled)
    {
        _preferences = _preferences with { SoundEnabled = enabled, SoundLevel = enabled ? 2 : 0 };
        SavePreferences();
        ApplyPreferences();
        if (enabled) _sounds.Play();
        ShowMorningBrief();
    }

    private void CycleSoundLevel()
    {
        int next = (_preferences.SoundLevel + 1) % 3;
        _preferences = _preferences with { SoundLevel = next, SoundEnabled = next > 0 };
        SavePreferences();
        ApplyPreferences();
        if (next > 0) _sounds.Play();
        ShowMorningBrief();
    }

    private void SavePreferences() => _preferencesService.Save(PreferencesPath, _preferences);

    private void ApplyPreferences()
    {
        GetWindow().ContentScaleFactor = _preferences.TextScale;
        _sounds.SetLevel(_preferences.SoundLevel);
        _parkWorld?.SetReducedMotion(_preferences.ReducedMotion);
    }

    private void StartDay()
    {
        HashSet<string> beforeStart = _unlocksAtDayStart;
        _commands.Execute(new PlanDayCommand((long)Math.Round(_price.Value * 100), (int)_batch.Value));
        _commands.Execute(new StartLiveCommand());
        _sounds.Play();
        ShowLive();
        string[] newlyInstalled = _state.Progression.Unlocks.Where(id => !beforeStart.Contains(id)).ToArray();
        if (newlyInstalled.Length > 0)
        {
            string label = string.Join(" · ", newlyInstalled.Select(UpgradeDisplayName));
            ShowToast($"Installed: {label}");
        }
        _unlocksAtDayStart = _state.Progression.Unlocks.ToHashSet(StringComparer.Ordinal);
    }

    private VBoxContainer BuildSparklineCard(int currentDayIndex)
    {
        long[] revenueByDay = new long[7];
        int startDay = Math.Max(0, currentDayIndex - 6);
        for (int day = startDay; day <= currentDayIndex; day++)
        {
            long revenue = _state.Business.Ledger
                .Where(e => e.DayIndex == day && e.Type == Zest.Domain.Economy.LedgerEntryType.Sale)
                .Sum(e => e.Amount.MinorUnits);
            revenueByDay[day - startDay] = revenue;
        }
        VBoxContainer card = Card("RECENT REVENUE",
            $"Last {currentDayIndex - startDay + 1} day{(currentDayIndex - startDay > 0 ? "s" : "")} of sales.");
        RevenueSparkline chart = new()
        {
            Values = revenueByDay,
            FilledCount = currentDayIndex - startDay + 1,
            CustomMinimumSize = new Vector2(0, 68),
        };
        card.AddChild(chart);
        return card;
    }

    private partial class RevenueSparkline : Control
    {
        public long[] Values { get; init; } = [];
        public int FilledCount { get; init; }

        public override void _Draw()
        {
            if (Values.Length == 0 || Size.X <= 0 || Size.Y <= 0) return;
            long max = Math.Max(1, Values.Max());
            float w = Size.X, h = Size.Y;
            float slot = w / Values.Length;
            float barWidth = Mathf.Max(4, slot - 6);
            Color filled = ZestStyle.Palette.ZestYellow;
            Color pending = new(1, 1, 1, 0.18f);
            for (int i = 0; i < Values.Length; i++)
            {
                float ratio = Values[i] / (float)max;
                float barH = ratio * (h - 12);
                float x = i * slot + (slot - barWidth) / 2f;
                float y = h - barH;
                Color c = i < FilledCount ? filled : pending;
                DrawRect(new Rect2(x, y, barWidth, barH), c);
                if (i < FilledCount && Values[i] > 0)
                {
                    // small dot at top marks the value
                    DrawRect(new Rect2(x, y - 2, barWidth, 2), c);
                }
            }
            // baseline
            DrawLine(new Vector2(0, h - 1), new Vector2(w, h - 1), new Color(ZestStyle.Palette.MutedInk, .4f), 1);
        }
    }

    private string BuildTodaysNote()
    {
        var park = _config.Locations.Values.First();
        var peak = park.TrafficCurve.OrderByDescending(t => t.OpportunitiesPerHour).First();
        int peakHour = 8 + ((peak.Hour - 8 + 24) % 24);
        // In-game clock shows sim hours 08:00–18:00 mapped from SimTime; display peak as HH:00 wall-clock.
        int shownHour = peak.Hour;
        return $"Foot traffic peaks around {shownHour:00}:00 — plan opening batch and prep timing for that window. Keep enough Classic ready without tying up every lemon.";
    }

    private static string UpgradeDisplayName(string id) => id switch
    {
        UpgradeIds.BetterCounter => "Better Counter (−10s prep)",
        UpgradeIds.ElectricJuicer => "Electric Juicer (−10s all drinks)",
        UpgradeIds.BiggerCooler => "Bigger Cooler (2× freshness)",
        _ => id,
    };

    private string SavePath => Path.Combine(ProjectSettings.GlobalizePath("user://"), "zest-day-boundary.json");
    private string PreferencesPath => Path.Combine(ProjectSettings.GlobalizePath("user://"), "zest-preferences.json");

    private void SaveMorning()
    {
        _saves.Save(SavePath, _state);
        _sounds.Play();
        ShowToast("Morning saved locally.");
    }

    private void LoadMorning()
    {
        try
        {
            GameState restored = _saves.Load(SavePath);
            if (restored.ConfigVersion != _config.Version) throw new InvalidDataException("Saved run uses a different content version.");
            _state = restored;
            _runner = ConfigLiveSessionFactory.CreateRunner(_state, _config, new DateOnly(2026, 9, 10));
            _commands = new DayCommandProcessor(_state, _runner);
            _clock = new SimulationClock(_state);
            _sounds.Play();
            switch (_state.DayCycle.Phase)
            {
                case DayPhase.Live:
                    // Mid-day quicksave: drop any partial queue and resume the day from the saved sim time.
                    _state.Operations.ClearActiveWork();
                    _runner.ResumeMidDay();
                    ShowLive();
                    ShowToast("Quicksave restored — queue reset for this hour.");
                    break;
                case DayPhase.Report:
                    ShowReport(_state.DayCycle.Report!);
                    ShowToast("Quicksave restored (end of day).");
                    break;
                default:
                    ShowMorningBrief();
                    ShowToast("Saved morning restored.");
                    break;
            }
        }
        catch (Exception error) when (error is IOException or InvalidDataException or NotSupportedException)
        {
            ShowToast($"Could not load save: {error.Message}");
        }
    }

    private void ShowLive()
    {
        ClearPage();
        UseWorldFirstChrome();
        SetProcess(true);
        Control live = new() { Name = "LiveOverlayLayer", SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = ZestStyle.PixelRendering.BaseResolution };
        _page.AddChild(live);

        _parkWorld = new ParkWorldView { Name = "ParkWorldView" };
        _parkWorld.ZIndex = 0;
        _parkWorld.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _parkWorld.WorldSelectionRequested += HandleWorldSelection;
        live.AddChild(_parkWorld);
        _parkWorld.SetReducedMotion(_preferences.ReducedMotion);
        BuildCornerHud(live);
        BuildContextPanel(live);
        BuildManagementPanel(live);
        BuildTimeControls(live);
        BuildHotbar(live);
        BuildToast(live);
        _juiceLayer = new Control { Name = "JuiceLayer", MouseFilter = MouseFilterEnum.Ignore, ZIndex = 25 };
        _juiceLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        live.AddChild(_juiceLayer);
        _observedSaleLedgerCount = _state.Business.Ledger.Count(entry => entry.DayIndex == _state.DayIndex && entry.Type == Zest.Domain.Economy.LedgerEntryType.Sale);
        _observedLossCount = _state.Operations.LossEvents.Count(loss => loss.DayIndex == _state.DayIndex);
        RefreshLive();
    }

    private void BuildCornerHud(Control live)
    {
        PanelContainer left = OverlayPanel(new Color(Night, .91f));
        left.SetAnchorsPreset(LayoutPreset.TopLeft);
        left.Position = new Vector2(20, 20);
        left.CustomMinimumSize = new Vector2(270, 84);
        VBoxContainer leftLines = new();
        leftLines.AddThemeConstantOverride("separation", ZestStyle.Space.Xs);
        leftLines.AddChild(HudLine(HudGlyph.Clock, out _dayTime));
        leftLines.AddChild(HudLine(HudGlyph.Sun, out _weather));
        left.AddChild(leftLines);
        ZestUiSkin.Tooltip(left, "Current day, local park time and weather");
        live.AddChild(left);

        PanelContainer right = OverlayPanel(new Color(Night, .91f));
        right.SetAnchorsPreset(LayoutPreset.TopRight);
        right.OffsetLeft = -330;
        right.OffsetRight = -20;
        right.OffsetTop = 20;
        right.OffsetBottom = 104;
        VBoxContainer rightLines = new();
        rightLines.AddThemeConstantOverride("separation", ZestStyle.Space.Xs);
        rightLines.AddChild(HudLine(HudGlyph.Coin, out _cashDelta, alignRight: true));
        rightLines.AddChild(HudLine(HudGlyph.Reputation, out _reputation, alignRight: true));
        right.AddChild(rightLines);
        ZestUiSkin.Tooltip(right, "Cash movement and local reputation");
        live.AddChild(right);
    }

    private static HBoxContainer HudLine(HudGlyph glyph, out Label label, bool alignRight = false)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", ZestStyle.Space.Sm);
        PixelStatusIcon icon = new() { Glyph = glyph };
        label = LabelText("", ZestStyle.Type.Action, Cream);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.HorizontalAlignment = alignRight ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        if (alignRight)
        {
            row.AddChild(label);
            row.AddChild(icon);
        }
        else
        {
            row.AddChild(icon);
            row.AddChild(label);
        }
        return row;
    }

    private void BuildContextPanel(Control live)
    {
        _contextPanel = OverlayPanel(new Color(Cream, .96f));
        _contextPanel.SetAnchorsPreset(LayoutPreset.CenterRight);
        _contextPanel.OffsetLeft = -330;
        _contextPanel.OffsetRight = -20;
        _contextPanel.OffsetTop = -205;
        _contextPanel.OffsetBottom = 170;
        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", 10);
        _contextTitle = LabelText("STAND", 20, Ink);
        content.AddChild(_contextTitle);
        _contextBody = new VBoxContainer();
        _contextBody.AddThemeConstantOverride("separation", 8);
        content.AddChild(_contextBody);
        Button close = CompactButton("Return to world", Paper, Ink);
        close.Pressed += ReturnToWorld;
        content.AddChild(close);
        _contextPanel.AddChild(content);
        _contextPanel.Visible = false;
        live.AddChild(_contextPanel);
    }

    private void BuildManagementPanel(Control live)
    {
        _managementPanel = OverlayPanel(new Color(Paper, .98f));
        _managementPanel.SetAnchorsPreset(LayoutPreset.Center);
        _managementPanel.OffsetLeft = -310;
        _managementPanel.OffsetRight = 310;
        _managementPanel.OffsetTop = -190;
        _managementPanel.OffsetBottom = 190;
        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", 14);
        _managementTitle = LabelText("NOTEBOOK", 26, Ink);
        content.AddChild(_managementTitle);
        _managementBody = new RichTextLabel { Text = "", BbcodeEnabled = false, FitContent = false, ScrollActive = true };
        _managementBody.AddThemeFontSizeOverride("normal_font_size", ZestStyle.Type.Body);
        _managementBody.AddThemeColorOverride("default_color", ZestStyle.Palette.MutedInk);
        _managementBody.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _managementBody.SizeFlagsVertical = SizeFlags.ExpandFill;
        content.AddChild(_managementBody);
        _managementCloseButton = CompactButton("Return to world", Citrus, Ink);
        _managementCloseButton.Pressed += ReturnFromManagement;
        content.AddChild(_managementCloseButton);
        _managementPanel.AddChild(content);
        _managementPanel.Visible = false;
        live.AddChild(_managementPanel);
    }

    private void BuildTimeControls(Control live)
    {
        _speedButtons.Clear();
        _timePanel = OverlayPanel(new Color(Night, .93f));
        _timePanel.SetAnchorsPreset(LayoutPreset.BottomLeft);
        _timePanel.OffsetLeft = 20;
        _timePanel.OffsetRight = 390;
        _timePanel.OffsetTop = -128;
        _timePanel.OffsetBottom = -20;
        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", 7);
        HBoxContainer controls = new();
        controls.AddThemeConstantOverride("separation", 6);
        controls.AddChild(SpeedButton("Ⅱ", SimulationSpeed.Paused));
        controls.AddChild(SpeedButton("1×", SimulationSpeed.Normal));
        controls.AddChild(SpeedButton("2×", SimulationSpeed.Double));
        controls.AddChild(SpeedButton("4×", SimulationSpeed.Quadruple));
        SelectSpeed(SimulationSpeed.Normal);
        _speedLabel = LabelText("1×", ZestStyle.Type.Label, ZestStyle.Palette.HudMuted);
        _speedLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _speedLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _speedLabel.VerticalAlignment = VerticalAlignment.Center;
        controls.AddChild(_speedLabel);
        content.AddChild(controls);
        HBoxContainer timelineLabels = new();
        Label openTime = LabelText("OPEN 08:00", ZestStyle.Type.Label, ZestStyle.Palette.HudMuted);
        openTime.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        timelineLabels.AddChild(openTime);
        timelineLabels.AddChild(LabelText("CLOSE 18:00", ZestStyle.Type.Label, ZestStyle.Palette.HudMuted));
        content.AddChild(timelineLabels);
        _dayProgress = new ProgressBar { MinValue = 0, MaxValue = 1, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 7) };
        _dayProgress.AddThemeStyleboxOverride("background", Box(new Color("#485956"), 3, 0, 0, Night));
        _dayProgress.AddThemeStyleboxOverride("fill", Box(Citrus, 3, 0, 0, Citrus));
        content.AddChild(_dayProgress);
        _timePanel.AddChild(content);
        ZestUiSkin.Tooltip(_timePanel, "Live-day clock · Space pauses or resumes");
        live.AddChild(_timePanel);

        Button quicksave = CompactButton("Quicksave", Paper, Ink);
        quicksave.ZIndex = 10;
        quicksave.SetAnchorsPreset(LayoutPreset.BottomRight);
        quicksave.OffsetLeft = -205;
        quicksave.OffsetRight = -20;
        quicksave.OffsetTop = -130;
        quicksave.OffsetBottom = -90;
        quicksave.Pressed += () =>
        {
            try { _saves.Save(SavePath, _state); _sounds.Play(); ShowToast("Quicksave written."); }
            catch (Exception e) when (e is IOException) { ShowToast($"Save failed: {e.Message}"); }
        };
        ZestUiSkin.Tooltip(quicksave, "Save the current day so you can resume later. Active queue is dropped on reload.");
        live.AddChild(quicksave);

        _endDayButton = CompactButton("Close the stand  →", Rust, Cream);
        _endDayButton.ZIndex = 10;
        _endDayButton.SetAnchorsPreset(LayoutPreset.BottomRight);
        _endDayButton.OffsetLeft = -205;
        _endDayButton.OffsetRight = -20;
        _endDayButton.OffsetTop = -78;
        _endDayButton.OffsetBottom = -20;
        _endDayButton.Pressed += CloseDay;
        _endDayButton.Visible = false;
        live.AddChild(_endDayButton);
    }

    private void BuildHotbar(Control live)
    {
        _hotbarButtons.Clear();
        PanelContainer panel = OverlayPanel(new Color(Cream, .95f));
        panel.SetAnchorsPreset(LayoutPreset.CenterBottom);
        panel.OffsetLeft = -250;
        panel.OffsetRight = 250;
        panel.OffsetTop = -76;
        panel.OffsetBottom = -20;
        HBoxContainer bar = new();
        bar.AddThemeConstantOverride("separation", 7);
        bar.AddChild(HotbarButton(LiveDestination.Stand, "STAND", ShowStandContext));
        bar.AddChild(HotbarButton(LiveDestination.Customers, "CUSTOMERS", () => ShowCustomerContext(0)));
        bar.AddChild(HotbarButton(LiveDestination.Notebook, "NOTEBOOK", () => ShowManagement(LiveDestination.Notebook)));
        bar.AddChild(HotbarButton(LiveDestination.Map, "MAP", () => ShowManagement(LiveDestination.Map)));
        panel.AddChild(bar);
        live.AddChild(panel);
    }

    private void BuildToast(Control live)
    {
        _toast = LabelText("", ZestStyle.Type.Label, Cream);
        _toast.ZIndex = 20;
        _toast.AddThemeStyleboxOverride("normal", Box(new Color(Night, .9f), ZestStyle.Radius.Pill, 9, 1, ZestStyle.Palette.River));
        _toast.SetAnchorsPreset(LayoutPreset.CenterTop);
        _toast.OffsetLeft = -240;
        _toast.OffsetRight = 240;
        _toast.OffsetTop = 24;
        _toast.OffsetBottom = 62;
        _toast.HorizontalAlignment = HorizontalAlignment.Center;
        _toast.VerticalAlignment = VerticalAlignment.Center;
        _toast.Visible = false;
        live.AddChild(_toast);
    }

    private Button SpeedButton(string label, SimulationSpeed speed)
    {
        Button button = CompactButton(label, new Color("#40514e"), Cream);
        button.CustomMinimumSize = new Vector2(48, 34);
        button.ToggleMode = true;
        _speedButtons[speed] = button;
        ZestUiSkin.Tooltip(button, speed == SimulationSpeed.Paused ? "Pause simulation" : $"Run simulation at {(int)speed}× speed");
        button.Pressed += () =>
        {
            SetSimulationSpeed(speed);
        };
        return button;
    }

    private void SetSimulationSpeed(SimulationSpeed speed)
    {
        _clock.SetSpeed(speed);
        _parkWorld?.SetSimulationSpeed((float)(int)speed);
        if (speed != SimulationSpeed.Paused) _resumeSpeed = speed;
        SelectSpeed(speed);
        RefreshTimeControls();
    }

    private void TogglePause() => SetSimulationSpeed(
        _clock.Speed == SimulationSpeed.Paused ? _resumeSpeed : SimulationSpeed.Paused);

    private void SelectSpeed(SimulationSpeed speed)
    {
        foreach ((SimulationSpeed value, Button button) in _speedButtons)
        {
            button.ButtonPressed = value == speed;
            ZestUiSkin.ApplyButton(button, value == speed ? Citrus : new Color("#40514e"), Cream);
        }
    }

    private void RefreshTimeControls()
    {
        string speed = _clock.Speed == SimulationSpeed.Paused ? "PAUSED" : $"RUNNING · {(int)_clock.Speed}×";
        _speedLabel.Text = _closingHourActive ? $"CLOSING · {speed}" : speed;
    }

    private Button HotbarButton(LiveDestination destination, string text, Action action)
    {
        Button button = CompactButton(text, Paper, Ink);
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        button.ToggleMode = true;
        _hotbarButtons[destination] = button;
        ZestUiSkin.Tooltip(button, text switch
        {
            "STAND" => "Open stand operations · 1",
            "CUSTOMERS" => "Observe the next waiting guest · 2",
            "NOTEBOOK" => "Open decisions and insights · N",
            _ => "Open the Riverside neighborhood map · M",
        });
        button.Pressed += action;
        return button;
    }

    private void SelectDestination(LiveDestination destination)
    {
        foreach ((LiveDestination value, Button button) in _hotbarButtons)
        {
            button.ButtonPressed = value == destination;
            ZestUiSkin.ApplyButton(button, value == destination ? Citrus : Paper, Ink);
        }
    }

    private void ReturnToWorld()
    {
        _contextPanel.Visible = false;
        _managementPanel.Visible = false;
        SelectDestination(LiveDestination.None);
        _currentContext = LiveDestination.None;
        _parkWorld.GrabFocus();
    }

    private void HandleWorldSelection(WorldSelection selection)
    {
        switch (selection.Kind)
        {
            case WorldSelectionKind.Stand: ShowStandContext(); break;
            case WorldSelectionKind.Customer: ShowCustomerContext(selection.Index); break;
            default: ReturnToWorld(); break;
        }
    }

    private void ShowStandContext()
    {
        _managementPanel.Visible = false;
        SelectDestination(LiveDestination.Stand);
        _currentContext = LiveDestination.Stand;
        ClearChildren(_contextBody);
        _contextTitle.Text = "ZEST STAND";
        DaySessionSnapshot snapshot = _commands.Snapshot();
        int extra = _state.Operations.Interventions.PreparedBatches.Sum(item => item.RemainingServings);
        int classic = _runner.SellableServings("classic");
        bool disabled = IsProductDisabled("classic");
        string classicCue = disabled ? "PAUSED" : classic == 0 ? "SOLD OUT" : "FRESH";
        Color classicCueColor = (disabled || classic == 0) ? Rust : Leaf;
        _contextBody.AddChild(ProductRow(HudGlyph.Lemon, $"CLASSIC · {Money(_runner.CurrentPrice("classic"))}", $"{classic} LEFT", classicCue, classicCueColor));
        if (_state.Progression.Has(MenuIds.Berry))
        {
            int berry = _runner.SellableServings("berry");
            bool berryDisabled = IsProductDisabled("berry");
            string berryCue = berryDisabled ? "PAUSED" : berry == 0 ? "SOLD OUT" : "FRESH";
            Color berryCueColor = (berryDisabled || berry == 0) ? Rust : Leaf;
            _contextBody.AddChild(ProductRow(HudGlyph.Berry, $"BERRY · {Money(_runner.CurrentPrice("berry"))}", $"{berry} LEFT", berryCue, berryCueColor));
        }
        else
        {
            _contextBody.AddChild(LabelText("BERRY  ·  NOT YET ON MENU", ZestStyle.Type.Label, ZestStyle.Palette.MutedInk));
        }
        if (_state.Progression.Has(MenuIds.Strong))
        {
            int strong = _runner.SellableServings("strong");
            bool strongDisabled = IsProductDisabled("strong");
            string strongCue = strongDisabled ? "PAUSED" : strong == 0 ? "SOLD OUT" : "FRESH";
            Color strongCueColor = (strongDisabled || strong == 0) ? Rust : Leaf;
            _contextBody.AddChild(ProductRow(HudGlyph.Lemon, $"STRONG · {Money(_runner.CurrentPrice("strong"))}", $"{strong} LEFT", strongCue, strongCueColor));
        }
        _contextBody.AddChild(LabelText("KEEP THE COUNTER READY. CHANGE THE PRICE ONLY WHEN DEMAND TELLS YOU TO.", ZestStyle.Type.Label, ZestStyle.Palette.MutedInk));
        HBoxContainer actions = new();
        actions.AddThemeConstantOverride("separation", 8);
        Button batch = CompactButton("Prep +6", Leaf, Cream);
        batch.Pressed += () => ExecuteIntervention(new PrepareExtraBatchCommand("classic", 6, 1800), "Six fresh Classic servings prepared");
        actions.AddChild(batch);
        long nextPrice = _runner.CurrentPrice("classic") == 350 ? 400 : 350;
        Button price = CompactButton(nextPrice == 400 ? "Raise price to $4" : "Return price to $3.50", Paper, Ink);
        price.Pressed += () => ExecuteIntervention(new ChangeLivePriceCommand("classic", nextPrice), "Classic price updated for new guests");
        actions.AddChild(price);
        _contextBody.AddChild(actions);
        Button inventory = CompactButton("Inventory details  →", Paper, Ink);
        inventory.Pressed += ShowInventoryDetails;
        ZestUiSkin.Tooltip(inventory, "Open detailed stock and delivery information");
        _contextBody.AddChild(inventory);
        _contextPanel.Visible = true;
    }

    public void PreviewOpenStandContext() => ShowStandContext();

    public void PreviewOpenCustomerContext() => ShowCustomerContext(0);

    public void PreviewOpenNotebook() => ShowManagement(LiveDestination.Notebook);

    public void PreviewOpenInventory() => ShowInventoryDetails();

    public void PreviewPauseClassic() => ExecuteIntervention(new TemporarilyDisableProductCommand("classic"), "Classic paused · coverage reduced");

    public void PreviewEmergencyRestock() => ExecuteIntervention(new RequestEmergencyRestockCommand("classic", 8, 120, 50, 1.75m), "Emergency delivery ordered · ETA 120s · premium paid");

    public void PreviewOpenDiagnostics() => ShowManagement(LiveDestination.Notebook);

    public void PreviewArt04(StandUpgradeVisual upgrade, StandOperatingVisual operatingState)
    {
        int stock = operatingState == StandOperatingVisual.SoldOut ? 0 : 18;
        _parkWorld.SetProductStatus(
            stock,
            disabled: false,
            rushMenu: operatingState == StandOperatingVisual.RushMenu,
            preparedBatchCount: operatingState == StandOperatingVisual.SoldOut ? 0 : 3,
            upgrade: upgrade);
    }

    private void ShowCustomerContext(int index)
    {
        _managementPanel.Visible = false;
        SelectDestination(LiveDestination.Customers);
        _currentContext = LiveDestination.Customers;
        _currentCustomerIndex = index;
        ClearChildren(_contextBody);
        Guid[] queue = _state.Operations.ActiveOrderIds.ToArray();
        if (index < 0 || index >= queue.Length || !_state.Operations.Orders.TryGetValue(queue[index], out OrderState? order))
        {
            ReturnToWorld();
            ShowToast("That guest has already left the queue.");
            return;
        }
        string customerId = order.CustomerId ?? queue[index].ToString("N");
        _contextTitle.Text = $"GUEST {customerId[..6].ToUpperInvariant()}";
        _contextBody.AddChild(LabelText("Waiting for Classic", ZestStyle.Type.Body, Ink));
        string guestRead = Guid.TryParse(customerId, out Guid customerGuid) && _state.Customers.Customers.TryGetValue(customerGuid, out CustomerState? customer)
            ? $"Segment  •  {customer.SegmentId}\nNeed  •  {customer.Need}\nNoticed stand  •  {(customer.Noticed ? "yes" : "no")}" 
            : "Live order is in service.\nCustomer history is unavailable.";
        _contextBody.AddChild(LabelText(guestRead, ZestStyle.Type.Action, ZestStyle.Palette.MutedInk));
        Button track = CompactButton("Pin customer", Night, Cream);
        track.Pressed += () =>
        {
            ShowToast(_parkWorld.TrackQueueCustomer(index) ? $"Tracking Guest {customerId[..6].ToUpperInvariant()}" : "That guest has already left the queue.");
        };
        _contextBody.AddChild(track);
        Button watchQueue = CompactButton("Watch queue", Paper, Ink);
        watchQueue.Pressed += () =>
        {
            _parkWorld.TrackQueueCustomer(0);
            ShowToast("Watching the live queue · outcomes unchanged");
        };
        _contextBody.AddChild(watchQueue);
        _contextPanel.Visible = true;
    }

    private void ShowManagement(LiveDestination destination)
    {
        _contextPanel.Visible = false;
        (_managementTitle.Text, _managementBody.Text) = destination switch
        {
            LiveDestination.Notebook => ("NOTEBOOK · LIVE DIAGNOSTICS", BuildDiagnosticNotebook()),
            LiveDestination.Map => ("NEIGHBORHOOD MAP", BuildNeighborhoodMap()),
            _ => throw new ArgumentOutOfRangeException(nameof(destination)),
        };
        _managementReturnDestination = LiveDestination.None;
        _managementCloseButton.Text = "Return to world";
        _managementPanel.Visible = true;
        SelectDestination(destination);
    }

    private string BuildDiagnosticNotebook()
    {
        OperationalDiagnosticSnapshot snapshot = _diagnostics.Observe(_state);
        string signals = string.Join("\n\n", snapshot.Signals.Select(signal =>
            $"{signal.Area} · {signal.Severity.ToString().ToUpperInvariant()}\n{signal.Headline}\n{signal.Evidence}"));
        return $"{signals}\n\nLOST CUSTOMER FEED\n{string.Join("\n", snapshot.LostCustomerFeed)}\n\nCOMPARE YESTERDAY\n{snapshot.YesterdayComparison}\n\nBOTTLENECK LENS\n{snapshot.Bottleneck}";
    }

    private string BuildNeighborhoodMap()
    {
        string weather = _runner.WeatherId?.ToUpperInvariant() ?? "PENDING";
        string temperature = _runner.TemperatureC is { } value ? $"{value:0.#}°C" : "—";
        int queue = _state.Operations.ActiveOrderIds.Count;
        return $"Riverside Park • West Gate\n\nWeather: {weather}  •  {temperature}\nLocal reputation: {_state.Progression.Reputation}/100\nActive queue: {queue}\n\nLocal context is available when requested, never as a permanent global tab.";
    }

    private void ShowInventoryDetails()
    {
        DaySessionSnapshot snapshot = _commands.Snapshot();
        int extra = _state.Operations.Interventions.PreparedBatches.Sum(item => item.RemainingServings);
        int total = _runner.SellableServings("classic");
        int berry = _runner.SellableServings("berry");
        _contextPanel.Visible = false;
        _managementTitle.Text = "INVENTORY DETAILS";
        _managementBody.Text = $"CLASSIC\n{total} servings ready  •  {_state.DayCycle.RemainingOpeningServings} opening batch  •  {extra} prepared during service\nFreshness: ready to sell\n\nBERRY\n{berry} servings ready  •  made to order\nStatus: {(berry > 0 ? "available" : "sold out")}";
        _managementReturnDestination = LiveDestination.Stand;
        _managementCloseButton.Text = "Back to stand";
        _managementPanel.Visible = true;
        SelectDestination(LiveDestination.Stand);
    }

    private void ReturnFromManagement()
    {
        if (_managementReturnDestination == LiveDestination.Stand)
        {
            ShowStandContext();
            return;
        }

        ReturnToWorld();
    }

    private static HBoxContainer ProductRow(HudGlyph glyph, string product, string quantity, string cue, Color cueColor)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 9);
        row.AddChild(new PixelStatusIcon { Glyph = glyph });
        Label name = LabelText(product, ZestStyle.Type.Action, Ink);
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(name);
        row.AddChild(LabelText(quantity, ZestStyle.Type.Label, Ink));
        Label status = LabelText(cue, ZestStyle.Type.Label, cueColor);
        status.AddThemeStyleboxOverride("normal", Box(new Color(cueColor, .12f), ZestStyle.Radius.Pill, 5, 1, new Color(cueColor, .45f)));
        row.AddChild(status);
        return row;
    }

    private void ExecuteIntervention(IDayCommand command, string success)
    {
        try
        {
            _commands.Execute(command);
            ShowToast(success);
            ShowStandContext();
            RefreshLive();
        }
        catch (InvalidOperationException error)
        {
            ShowToast(error.Message);
        }
    }

    private void ShowToast(string text)
    {
        if (_toast is null) return;
        _toast.Text = text;
        _toast.Visible = true;
    }

    private void RefreshLive()
    {
        DaySessionSnapshot snapshot = _commands.Snapshot();
        long capped = Math.Min(snapshot.SimTime, 10 * 60 * 60);
        long hours = capped / 3600;
        long minutes = capped % 3600 / 60;
        _dayTime.Text = $"DAY {snapshot.DayIndex + 1:00}  •  {8 + hours:00}:{minutes:00}";
        _weather.Text = _runner.WeatherId is { } weather && _runner.TemperatureC is { } temperature
            ? $"{weather.ToUpperInvariant()}  •  {temperature:0.#}°C"
            : "WEATHER PENDING";
        long todayDelta = snapshot.CashMinor - _state.Business.OpeningCashMinorUnits;
        _cashDelta.Text = $"{Money(snapshot.CashMinor)}  •  TODAY {SignedMoney(todayDelta)}";
        string reputationCue = _state.Progression.Reputation >= 65 ? "WARM" : _state.Progression.Reputation <= 35 ? "COOLING" : "STEADY";
        _reputation.Text = $"LOCAL REPUTATION  •  {reputationCue}  {_state.Progression.Reputation}/100";
        _dayProgress.Value = Math.Clamp(snapshot.SimTime / (10d * 60 * 60), 0, 1);
        bool closingHour = snapshot.SimTime >= 9 * 60 * 60;
        if (closingHour != _closingHourActive)
        {
            _closingHourActive = closingHour;
            _timePanel.AddThemeStyleboxOverride("panel", Box(
                closingHour ? new Color("#4f332b") : new Color(Night, .93f),
                ZestStyle.Radius.Control,
                12,
                closingHour ? 2 : 1,
                closingHour ? Citrus : new Color(ZestStyle.Palette.Border, .7f)));
        }
        _dayProgress.AddThemeStyleboxOverride("fill", Box(closingHour ? Rust : Citrus, 3, 0, 0, closingHour ? Rust : Citrus));
        _endDayButton.Visible = closingHour;
        if (snapshot.SimTime >= 10 * 60 * 60 && !_closeScheduled)
        {
            _closeScheduled = true;
            CallDeferred(nameof(CloseDay));
        }
        RefreshTimeControls();
        Guid[] queueCustomers = StartInLiveStudy
            ? Enumerable.Range(0, 5).Select(index => new Guid(index + 1, 0, 0, new byte[8])).ToArray()
            : _state.Operations.ActiveOrderIds.ToArray();
        Dictionary<Guid, QueueDepartureKind> departures = _state.Operations.Orders.Values
            .Where(order => order.Status is OrderStatus.Served or OrderStatus.Cancelled)
            .Where(order => _observedDepartures.Add(order.OrderId))
            .ToDictionary(order => order.OrderId, order => order.Status == OrderStatus.Cancelled ? QueueDepartureKind.Abandoned : QueueDepartureKind.Served);
        _parkWorld.SetQueueCustomers(queueCustomers, departures);
        ApplyRegularNameTags(queueCustomers);
        ApplyOrderBadges(queueCustomers);
        int stock = _runner.SellableServings("classic");
        int preparedBatches = _state.Operations.Interventions.PreparedBatches.Count(item => item.RemainingServings > 0);
        bool rushMenu = _state.Operations.Interventions.RushMenuProductIds.Count > 0;
        _parkWorld.SetProductStatus(stock, IsProductDisabled("classic"), rushMenu, preparedBatches, ResolveUpgradeVisual());
        _parkWorld.SetVendorPose(ResolveVendorPose(snapshot.SimTime));
        _parkWorld.SetWeather(_runner.WeatherId);
        _parkWorld.SetReputation(_state.Progression.Reputation);
        EmitSaleAndLossJuice();
        if (_contextPanel.Visible)
        {
            if (_currentContext == LiveDestination.Stand) ShowStandContext();
            else if (_currentContext == LiveDestination.Customers) ShowCustomerContext(_currentCustomerIndex);
        }
    }

    private void EmitSaleAndLossJuice()
    {
        if (_juiceLayer is null) return;

        var todaySales = _state.Business.Ledger
            .Where(entry => entry.DayIndex == _state.DayIndex && entry.Type == Zest.Domain.Economy.LedgerEntryType.Sale)
            .ToArray();
        int newSaleCount = todaySales.Length - _observedSaleLedgerCount;
        if (newSaleCount > 0)
        {
            long totalMinor = 0;
            for (int i = _observedSaleLedgerCount; i < todaySales.Length; i++)
                totalMinor += todaySales[i].Amount.MinorUnits;
            _sounds.PlayCoin();
            SpawnFloater($"+{Money(totalMinor)}", ZestStyle.Palette.ZestYellow, anchorRight: true);
            _observedSaleLedgerCount = todaySales.Length;
        }

        var todayLosses = _state.Operations.LossEvents
            .Where(loss => loss.DayIndex == _state.DayIndex)
            .ToArray();
        int newLossCount = todayLosses.Length - _observedLossCount;
        if (newLossCount > 0)
        {
            var recent = todayLosses.Skip(_observedLossCount).ToArray();
            var dominant = recent.GroupBy(l => l.Reason).OrderByDescending(g => g.Count()).First().Key;
            _sounds.PlayWalkaway();
            SpawnFloater(LossReasonBadge(dominant), ZestStyle.Palette.Rust, anchorRight: false);
            _observedLossCount = todayLosses.Length;
        }
    }

    private void ApplyOrderBadges(Guid[] queueCustomers)
    {
        foreach (Guid id in queueCustomers)
        {
            if (id == Guid.Empty || !_state.Operations.Orders.TryGetValue(id, out OrderState? order))
            {
                _parkWorld.SetCustomerOrderBadge(id, null);
                continue;
            }
            string recipeId = order.RecipeVersionId.RecipeId;
            (string label, Color bg, Color text) = recipeId switch
            {
                "berry" => ("B", new Color(0.62f, 0.28f, 0.58f, .88f), Cream),
                "strong" => ("S", new Color(0.85f, 0.55f, 0.15f, .90f), Night),
                _ => ("L", new Color(ZestStyle.Palette.ZestYellow, .88f), Night),
            };
            _parkWorld.SetCustomerOrderBadge(id, label, text, bg);
        }
    }

    private void ApplyRegularNameTags(Guid[] queueCustomers)
    {
        foreach (Guid id in queueCustomers)
        {
            if (id == Guid.Empty) continue;
            if (!_customerRegularName.TryGetValue(id, out string? name))
            {
                // ~1 in 4 queue guests are a named regular; deterministic from id.
                int hash = id.GetHashCode();
                uint u = unchecked((uint)hash);
                if (u % 4 != 0) { _customerRegularName[id] = ""; continue; }
                name = RegularNames[(int)((u / 4) % (uint)RegularNames.Length)];
                _customerRegularName[id] = name;
                int visit = _regularVisits.GetValueOrDefault(name) + 1;
                _regularVisits[name] = visit;
                if (visit >= 2 && _greetedThisDay.Add(name))
                    ShowToast($"{name} is back  ·  visit {visit}");
            }
            if (!string.IsNullOrEmpty(name))
            {
                int visits = _regularVisits.GetValueOrDefault(name);
                _parkWorld.SetCustomerNameTag(id, visits > 1 ? $"{name} ·{visits}" : name);
            }
        }
    }

    private static string LossReasonBadge(Zest.Domain.Customers.LostSaleReason reason) => reason switch
    {
        Zest.Domain.Customers.LostSaleReason.PriceTooHigh => "TOO EXPENSIVE",
        Zest.Domain.Customers.LostSaleReason.QueueAbandonment => "GAVE UP WAITING",
        Zest.Domain.Customers.LostSaleReason.OutsideOption => "WALKED PAST",
        Zest.Domain.Customers.LostSaleReason.PoorProductFit => "NOT INTERESTED",
        Zest.Domain.Customers.LostSaleReason.ClosingTime => "MISSED · CLOSED",
        Zest.Domain.Customers.LostSaleReason.NoNeed => "NOT THIRSTY",
        Zest.Domain.Customers.LostSaleReason.NoticedNothing => "DIDN'T NOTICE",
        _ => "LOST",
    };

    private void SpawnFloater(string text, Color color, bool anchorRight)
    {
        Label floater = LabelText(text, ZestStyle.Type.Action, color);
        floater.AddThemeStyleboxOverride("normal", Box(new Color(Night, .82f), ZestStyle.Radius.Pill, 8, 1, new Color(color, .7f)));
        floater.HorizontalAlignment = HorizontalAlignment.Center;
        floater.VerticalAlignment = VerticalAlignment.Center;
        floater.SetAnchorsPreset(anchorRight ? LayoutPreset.TopRight : LayoutPreset.TopLeft);
        floater.OffsetLeft = anchorRight ? -260 : 40;
        floater.OffsetRight = anchorRight ? -60 : 260;
        floater.OffsetTop = 108;
        floater.OffsetBottom = 146;
        floater.MouseFilter = MouseFilterEnum.Ignore;
        floater.Modulate = new Color(1, 1, 1, 0);
        _juiceLayer.AddChild(floater);
        Tween tween = floater.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(floater, "modulate:a", 1f, .12f);
        tween.TweenProperty(floater, "offset_top", 78f, .55f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(floater, "offset_bottom", 116f, .55f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.SetParallel(false);
        tween.TweenInterval(.55);
        tween.TweenProperty(floater, "modulate:a", 0f, .35f);
        tween.TweenCallback(Callable.From(floater.QueueFree));
    }

    private VendorPose ResolveVendorPose(long simTime)
    {
        OrderState? order = _state.Operations.ActiveOrderIds
            .Select(id => _state.Operations.Orders[id])
            .FirstOrDefault();
        if (order is null) return VendorPose.Idle;
        if (order.Status == OrderStatus.Ready) return VendorPose.Handoff;
        if (order.Status != OrderStatus.InProgress) return VendorPose.Idle;

        WorkTaskState? task = order.TaskIds
            .Select(id => _state.Operations.Tasks[id])
            .FirstOrDefault(item => item.Status == WorkTaskStatus.Running);
        if (task?.StartedAtSimTime is not long started || task.CompletedAtSimTime is not long completed || completed <= started)
            return VendorPose.Prepare;
        double progress = Math.Clamp((simTime - started) / (double)(completed - started), 0, 1);
        return progress < .65 ? VendorPose.Prepare : VendorPose.Pour;
    }

    private StandUpgradeVisual ResolveUpgradeVisual() =>
        _state.Progression.Has(UpgradeIds.BiggerCooler) ? StandUpgradeVisual.BiggerCooler :
        _state.Progression.Has(UpgradeIds.ElectricJuicer) ? StandUpgradeVisual.ElectricJuicer :
        _state.Progression.Has(UpgradeIds.BetterCounter) ? StandUpgradeVisual.BetterCounter : StandUpgradeVisual.Base;

    private bool IsProductDisabled(string productId) =>
        _state.Operations.Interventions.DisabledUntil.TryGetValue(productId, out long until) && _state.SimTime < until;

    private void CloseDay()
    {
        _clock.SetSpeed(SimulationSpeed.Paused);
        SetProcess(false);
        DaySessionSnapshot snapshot = _commands.Execute(new CloseDayCommand());
        _sounds.Play();
        ShowReport(snapshot.Report!);
    }

    public void PreviewClosingHour()
    {
        _clock.SetSpeed(SimulationSpeed.Quadruple);
        _clock.Advance(TimeSpan.FromSeconds(9 * 60 * 60 / 40d));
        _clock.SetSpeed(SimulationSpeed.Paused);
        SelectSpeed(SimulationSpeed.Paused);
        RefreshLive();
    }

    private void ShowReport(DailyReportSnapshot report)
    {
        ClearPage();
        UseEditorialChrome();
        _phaseLabel.Text = "DAILY REPORT";
        _page.AddChild(LabelText("What the day taught us", ZestStyle.Type.Display, Ink));
        _page.AddChild(LabelText("A quiet accounting of outcomes — not a scoreboard.", ZestStyle.Type.Body, ZestStyle.Palette.MutedInk));
        HBoxContainer cards = new();
        cards.AddThemeConstantOverride("separation", 14);
        AddReportCard(cards, "REVENUE", Money(report.RevenueMinor));
        AddReportCard(cards, "PROFIT", Money(report.ProfitMinor));
        AddReportCard(cards, "SALES", report.SalesCount.ToString());
        AddReportCard(cards, "QUEUE LOSSES", report.QueueLossCount.ToString());
        _page.AddChild(cards);
        _page.AddChild(BuildSparklineCard(report.DayIndex));
        _page.AddChild(Card("EDITOR'S NOTE", BuildReportInsight(report)));
        Button nextDay = ActionButton("Start next day  →", Citrus, Ink);
        nextDay.Pressed += StartNextDay;
        _page.AddChild(nextDay);
        if (!_state.Progression.Has(MenuIds.Berry))
        {
            VBoxContainer unlockCard = Card("NEW RECIPE  ·  BERRY LEMONADE",
                "A slightly premium variant. Higher price, higher taste weight for some segments — but it eats one more lemon per serving. Try it tomorrow?");
            Button addBerry = CompactButton("Add Berry to menu", Leaf, Cream);
            addBerry.Pressed += () =>
            {
                _commands.Execute(new AddBerryToMenuCommand());
                ShowToast("Berry Lemonade added to tomorrow's menu.");
                ShowReport(_commands.Snapshot().Report!);
            };
            unlockCard.AddChild(addBerry);
            _page.AddChild(unlockCard);
        }
        else if (!_state.Progression.Has(MenuIds.Strong))
        {
            VBoxContainer unlockCard = Card("NEW RECIPE  ·  STRONG LEMONADE",
                "A double-lemon variant priced at $5. Commuters gravitate to it during their rush hours. Uses more of your lemon budget — a hypothesis worth testing?");
            Button addStrong = CompactButton("Add Strong to menu", Leaf, Cream);
            addStrong.Pressed += () =>
            {
                _commands.Execute(new AddStrongToMenuCommand());
                ShowToast("Strong Lemonade added to tomorrow's menu.");
                ShowReport(_commands.Snapshot().Report!);
            };
            unlockCard.AddChild(addStrong);
            _page.AddChild(unlockCard);
        }
        _page.AddChild(UpgradeButton(UpgradeIds.BetterCounter, "Better counter", "$5.00", ProgressionService.BetterCounterCostMinor,
            new PurchaseBetterCounterCommand(), "Classic preparation takes 10 seconds less."));
        _page.AddChild(UpgradeButton(UpgradeIds.ElectricJuicer, "Electric juicer", "$8.00", ProgressionService.ElectricJuicerCostMinor,
            new PurchaseElectricJuicerCommand(), "All drink preparation takes 10 seconds less."));
        _page.AddChild(UpgradeButton(UpgradeIds.BiggerCooler, "Bigger cooler", "$6.00", ProgressionService.BiggerCoolerCostMinor,
            new PurchaseBiggerCoolerCommand(), "Prepared batches stay fresh twice as long."));
    }

    private void StartNextDay()
    {
        _commands.Execute(new StartNextDayCommand());
        _closeScheduled = false;
        _closingHourActive = false;
        _observedDepartures.Clear();
        _observedSaleLedgerCount = 0;
        _observedLossCount = 0;
        _greetedThisDay.Clear();
        _clock = new SimulationClock(_state);
        _sounds.Play();
        ShowMorningBrief();
    }

    private VBoxContainer UpgradeButton(string id, string name, string price, long cost, IDayCommand command, string effect)
    {
        bool owned = _state.Progression.Has(id);
        VBoxContainer item = new();
        Button button = CompactButton(owned ? name + " installed" : "Buy " + name + " · " + price, owned ? Leaf : Paper, owned ? Cream : Ink);
        button.Disabled = owned || _state.Business.CashMinorUnits < cost;
        button.Pressed += () =>
        {
            _commands.Execute(command);
            ShowReport(_commands.Snapshot().Report!);
        };
        item.AddChild(button);
        item.AddChild(LabelText(name + ": " + effect, ZestStyle.Type.Action, ZestStyle.Palette.MutedInk));
        return item;
    }

    private static PanelContainer OverlayPanel(Color fill)
    {
        return ZestUiSkin.Panel(fill);
    }

    private string BuildReportInsight(DailyReportSnapshot report)
    {
        var todayLosses = _state.Operations.LossEvents
            .Where(loss => loss.DayIndex == report.DayIndex)
            .ToArray();
        int closing = todayLosses.Count(l => l.Reason == Zest.Domain.Customers.LostSaleReason.ClosingTime);
        int abandonment = todayLosses.Count(l => l.Reason == Zest.Domain.Customers.LostSaleReason.QueueAbandonment);
        int priceLoss = todayLosses.Count(l => l.Reason == Zest.Domain.Customers.LostSaleReason.PriceTooHigh);
        int noInterest = todayLosses.Count(l => l.Reason == Zest.Domain.Customers.LostSaleReason.PoorProductFit || l.Reason == Zest.Domain.Customers.LostSaleReason.OutsideOption);
        int notNoticed = todayLosses.Count(l => l.Reason == Zest.Domain.Customers.LostSaleReason.NoticedNothing);

        var lines = new List<string>();
        lines.Add($"{report.SalesCount} served · {todayLosses.Length} lost · {Money(report.RevenueMinor)} revenue.");

        if (closing > 0)
            lines.Add($"{closing} guest{(closing == 1 ? "" : "s")} still waited at closing — a faster counter or a narrower rush menu would protect tomorrow's line.");
        else if (abandonment >= 3)
            lines.Add($"{abandonment} guests gave up in the queue. Wait time, not demand, was today's ceiling.");
        else if (priceLoss >= 3)
            lines.Add($"{priceLoss} guests balked at the price. A test cut for a single hour would tell you if it's the number or the fit.");
        else if (noInterest >= 3)
            lines.Add($"{noInterest} guests weren't hooked by the menu. Consider a variant or a signage change before touching price.");
        else if (report.RemainingStock == 0 && report.SalesCount > 0)
            lines.Add("Sold through the visible menu. Bigger opening batch tomorrow — or a small price nudge — turns a stockout into revenue.");
        else if (report.SalesCount == 0)
            lines.Add("The stand stayed open but nothing converted. Review price and product fit before buying more capacity.");
        else if (notNoticed >= 5)
            lines.Add($"{notNoticed} passersby didn't even notice the stand. Signage or foot-traffic timing matters more than menu today.");
        else
            lines.Add("Steady day with no dominant complaint. Tomorrow is a good day to test a price or a variant deliberately.");

        return string.Join("\n\n", lines);
    }

    private static VBoxContainer Card(string kicker, string body)
    {
        PanelContainer panel = OverlayPanel(Cream);
        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", 12);
        content.AddChild(LabelText(kicker, ZestStyle.Type.Label, Rust));
        Label copy = LabelText(body, ZestStyle.Type.Body, Ink);
        copy.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(copy);
        panel.AddChild(content);
        VBoxContainer wrapper = new();
        wrapper.AddChild(panel);
        return wrapper;
    }

    private static void AddReportCard(HBoxContainer row, string title, string value)
    {
        VBoxContainer card = Card(title, value);
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(card);
    }

    private static Label LabelText(string text, int size, Color color)
    {
        Label label = new() { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Button ActionButton(string text, Color fill, Color textColor)
    {
        Button button = CompactButton(text, fill, textColor);
        button.CustomMinimumSize = new Vector2(170, 46);
        return button;
    }

    private static Button CompactButton(string text, Color fill, Color textColor)
    {
        Button button = new() { Text = text, CustomMinimumSize = new Vector2(76, 36) };
        ZestUiSkin.ApplyButton(button, fill, textColor);
        return button;
    }

    private static StyleBoxFlat Box(Color fill, int radius, int padding, int border, Color borderColor)
    {
        return ZestUiSkin.Frame(fill, radius, padding, border, borderColor);
    }

    private void ClearPage()
    {
        foreach (Node child in _page.GetChildren()) child.QueueFree();
    }

    private static void ClearChildren(Node parent)
    {
        foreach (Node child in parent.GetChildren()) child.QueueFree();
    }

    private static string Money(long minor) => FormattableString.Invariant($"${minor / 100m:0.00}");
    private static string SignedMoney(long minor) => FormattableString.Invariant($"{(minor >= 0 ? "+" : "-")}${Math.Abs(minor) / 100m:0.00}");
}
