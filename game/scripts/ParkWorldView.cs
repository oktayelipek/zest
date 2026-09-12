using Godot;

namespace Zest.Game;

public enum WorldSelectionKind { None, Stand, Customer }
public enum QueueDepartureKind { Served, Abandoned }
public readonly record struct WorldSelection(WorldSelectionKind Kind, int Index = -1);

/// <summary>ART-12 playable 2D pixel slice. Simulation state remains outside Godot.</summary>
public partial class ParkWorldView : SubViewportContainer
{
    public enum CameraFraming { Business, Neighborhood, CloseObservation }

    private readonly List<HdCustomerActor> _queuePeople = [];
    private readonly Dictionary<Guid, HdCustomerActor> _queuePeopleById = [];
    private SubViewport _viewport = null!;
    private Camera2D _camera = null!;
    private ProductionParkCanvas _park = null!;
    private Node2D _queueRoot = null!;
    private readonly List<HdCustomerActor> _ambientWalkers = [];
    private Line2D _focusMarker = null!;
    private PixelWorldText _stockBoard = null!;
    private PixelWorldText _queueBoard = null!;
    private ColorRect _weatherTint = null!;
    private ColorRect _eveningTint = null!;
    private RainOverlay _rain = null!;
    private ReputationStars _stars = null!;
    private bool _eveningActive;
    private string? _lastWeatherId;
    private Vector2 _target;
    private CameraFraming _framing = CameraFraming.Business;
    private int _requestedQueueLength;
    private int? _pendingQueueLength;
    private Guid[]? _pendingCustomerIds;
    private IReadOnlyDictionary<Guid, QueueDepartureKind>? _pendingDepartures;
    private bool _customerLeaveInProgress;
    private float _simulationSpeed = 1f;
    private bool _reducedMotion;

    public event Action<WorldSelection>? WorldSelectionRequested;

    public override void _Ready()
    {
        Stretch = true;
        StretchShrink = ZestStyle.PixelRendering.WorldRenderScale;
        TextureFilter = TextureFilterEnum.Nearest;
        MouseDefaultCursorShape = CursorShape.Drag;
        BuildWorld();
        SetFraming(CameraFraming.Business);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && motion.ButtonMask.HasFlag(MouseButtonMask.Left))
        {
            Vector2 zoom = _camera.Zoom;
            _target -= motion.Relative / new Vector2(Mathf.Max(zoom.X, .01f), Mathf.Max(zoom.Y, .01f));
            _target = new Vector2(Mathf.Clamp(_target.X, -80, 80), Mathf.Clamp(_target.Y, -40, 44));
            ApplyCamera();
            AcceptEvent();
            return;
        }

        if (@event is not InputEventMouseButton mouse || !mouse.Pressed) return;
        if (mouse.ButtonIndex == MouseButton.Left)
        {
            SelectAt(mouse.Position);
            AcceptEvent();
        }
        else if (mouse.ButtonIndex == MouseButton.WheelUp)
            SetFraming((CameraFraming)Mathf.Min((int)_framing + 1, 2));
        else if (mouse.ButtonIndex == MouseButton.WheelDown)
            SetFraming((CameraFraming)Mathf.Max((int)_framing - 1, 0));
    }

    public void SetFraming(CameraFraming framing)
    {
        _framing = framing;
        _target = Vector2.Zero;
        ApplyCamera();
    }

    public void SetQueueLength(int length)
    {
        SetQueueCustomers(Enumerable.Range(0, length).Select(index => new Guid(index + 1, 0, 0, new byte[8])).ToArray());
    }

    private Func<Guid, string?>? _segmentResolver;

    public void SetQueueCustomers(IReadOnlyList<Guid> customerIds, IReadOnlyDictionary<Guid, QueueDepartureKind>? departures = null, Func<Guid, string?>? segmentResolver = null)
    {
        ArgumentNullException.ThrowIfNull(customerIds);
        _segmentResolver = segmentResolver;
        int clamped = Mathf.Clamp(customerIds.Count, 0, ProductionParkCanvas.QueuePositions.Length);
        if (_queueRoot is null) return;
        if (_customerLeaveInProgress)
        {
            _pendingQueueLength = clamped;
            _pendingCustomerIds = customerIds.Take(clamped).ToArray();
            _pendingDepartures = departures;
            return;
        }
        if (_queuePeople.Select(actor => actor.CustomerId).SequenceEqual(customerIds.Take(clamped))) return;

        // Preserve the visible post-sale beat before removing the first queue actor.
        if (clamped < _queuePeople.Count && _queuePeople.Count > 0)
        {
            _pendingQueueLength = clamped;
            _pendingCustomerIds = customerIds.Take(clamped).ToArray();
            _pendingDepartures = departures;
            _customerLeaveInProgress = true;
            Guid departing = _queuePeople.Select(actor => actor.CustomerId).FirstOrDefault(id => !customerIds.Contains(id));
            HdCustomerActor actor = _queuePeopleById.GetValueOrDefault(departing, _queuePeople[0]);
            actor.PlayDeparturePose(departures?.GetValueOrDefault(departing) == QueueDepartureKind.Abandoned);
            GetTree().CreateTimer(.8).Timeout += FinishCustomerLeave;
            return;
        }

        ReconcileQueue(customerIds.Take(clamped).ToArray());
    }

    private void FinishCustomerLeave()
    {
        _customerLeaveInProgress = false;
        int target = _pendingQueueLength ?? _requestedQueueLength;
        Guid[] ids = _pendingCustomerIds ?? Enumerable.Range(0, target).Select(index => new Guid(index + 1, 0, 0, new byte[8])).ToArray();
        _pendingQueueLength = null;
        _pendingCustomerIds = null;
        _pendingDepartures = null;
        ReconcileQueue(ids);
    }

    private void RebuildQueue(int clamped)
    {
        SetQueueCustomers(Enumerable.Range(0, clamped).Select(index => new Guid(index + 1, 0, 0, new byte[8])).ToArray());
    }

    private void ReconcileQueue(IReadOnlyList<Guid> customerIds)
    {
        _requestedQueueLength = customerIds.Count;
        HashSet<Guid> desired = customerIds.ToHashSet();
        foreach ((Guid id, HdCustomerActor actor) in _queuePeopleById.Where(pair => !desired.Contains(pair.Key)).ToArray())
        {
            actor.QueueFree();
            _queuePeopleById.Remove(id);
        }
        _queuePeople.Clear();
        for (int index = 0; index < customerIds.Count; index++)
        {
            Guid id = customerIds[index];
            if (!_queuePeopleById.TryGetValue(id, out HdCustomerActor? customer))
            {
                customer = new HdCustomerActor { Name = $"QueueGuest_{id:N}" };
                customer.Configure(walksRoute: false, index % 2, id, segmentId: _segmentResolver?.Invoke(id));
                customer.SetSimulationSpeed(_simulationSpeed);
                customer.SetReducedMotion(_reducedMotion);
                _queueRoot.AddChild(customer);
                _queuePeopleById.Add(id, customer);
            }
            customer.Position = ProductionParkCanvas.QueuePositions[index];
            _queuePeople.Add(customer);
        }

        _focusMarker.Visible = false;
        _queueBoard.SetText($"QUEUE {_requestedQueueLength}");
    }

    public void SetSimulationSpeed(float speed)
    {
        _simulationSpeed = Mathf.Max(0, speed);
        foreach (HdCustomerActor actor in _queuePeople) actor.SetSimulationSpeed(_simulationSpeed);
        foreach (HdCustomerActor walker in _ambientWalkers) walker.SetSimulationSpeed(_simulationSpeed);
    }

    public void SetReducedMotion(bool enabled)
    {
        _reducedMotion = enabled;
        foreach (HdCustomerActor actor in _queuePeople) actor.SetReducedMotion(enabled);
        foreach (HdCustomerActor walker in _ambientWalkers) walker.SetReducedMotion(enabled);
    }

    public void SetProductStatus(int quantity, bool disabled, bool rushMenu = false, int preparedBatchCount = 0, StandUpgradeVisual? upgrade = null)
    {
        if (_stockBoard is null) return;
        _stockBoard.SetText(disabled ? "CLASSIC OFF" : quantity <= 0 ? "SOLD OUT" : rushMenu ? $"RUSH {quantity}" : $"CLASSIC {quantity}");
        _stockBoard.SetColor(disabled || quantity <= 0 ? ZestStyle.Palette.Rust : ZestStyle.Palette.Cream);
        StandOperatingVisual operatingState = disabled || quantity <= 0
            ? StandOperatingVisual.SoldOut
            : rushMenu ? StandOperatingVisual.RushMenu : StandOperatingVisual.Normal;
        _park.Stand.SetPresentation(upgrade ?? _park.Stand.Upgrade, operatingState, preparedBatchCount);
    }

    public void SetVendorPose(VendorPose pose)
    {
        _park.Stand.SetVendorPose(pose);
        if (_queuePeople.Count > 0) _queuePeople[0].SetServicePose(pose == VendorPose.Handoff);
    }

    /// <summary>Starts the visible post-sale exit for the first waiting customer.</summary>
    public void PlayFirstCustomerLeave()
    {
        if (_queuePeople.Count == 0) return;
        _queuePeople[0].PlayLeavePose();
    }

    public void SetWeather(string? weatherId)
    {
        if (_weatherTint is null || _lastWeatherId == weatherId) return;
        _lastWeatherId = weatherId;
        Color tint = weatherId switch
        {
            "rain" => new Color(0.32f, 0.42f, 0.58f, 0.28f),
            "cloudy" => new Color(0.55f, 0.58f, 0.62f, 0.18f),
            "sunny" => new Color(1.0f, 0.85f, 0.55f, 0.10f),
            _ => new Color(0, 0, 0, 0),
        };
        Tween tween = _weatherTint.CreateTween();
        tween.TweenProperty(_weatherTint, "color", tint, 0.6);
        if (_rain is not null) _rain.SetActive(weatherId == "rain");
        _park?.Stand.SetWeatherOverlay(weatherId);
        _park?.Stand.SetVendorRainOverlay(weatherId == "rain");
    }

    public void SetStrongMenu(bool active) => _park?.Stand.SetStrongMenuFlag(active);

    public void SetEveningBackground(bool evening)
    {
        _park?.SetTimeOfDay(evening);
        if (_eveningTint is null || _eveningActive == evening) return;
        _eveningActive = evening;
        // Warm amber wash for the closing hours when no evening PNG is baked yet.
        Color target = evening ? new Color(1.0f, 0.62f, 0.32f, 0.16f) : new Color(0, 0, 0, 0);
        Tween tween = _eveningTint.CreateTween();
        tween.TweenProperty(_eveningTint, "color", target, 1.2);
    }

    public void SetReputation(int reputation)
    {
        int stars = reputation >= 80 ? 3 : reputation >= 60 ? 2 : reputation >= 35 ? 1 : 0;
        _stars?.SetStarCount(stars);
    }

    public void SetCustomerNameTag(Guid customerId, string? label)
    {
        if (_queuePeopleById.TryGetValue(customerId, out HdCustomerActor? actor))
            actor.SetNameTag(label);
    }

    public void SetCustomerOrderBadge(Guid customerId, string? label, Color? textColor = null, Color? background = null)
    {
        if (_queuePeopleById.TryGetValue(customerId, out HdCustomerActor? actor))
            actor.SetOrderBadge(label, textColor, background);
    }

    public bool TrackQueueCustomer(int index)
    {
        if (index < 0 || index >= _queuePeople.Count) return false;
        _framing = CameraFraming.CloseObservation;
        _target = _queuePeople[index].Position + new Vector2(0, -20);
        _focusMarker.Position = _queuePeople[index].Position + new Vector2(0, 3);
        _focusMarker.Visible = true;
        ApplyCamera();
        return true;
    }

    private void BuildWorld()
    {
        _viewport = new SubViewport
        {
            Size = ZestStyle.PixelRendering.LogicalWorldResolution,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            TransparentBg = false,
            CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest,
        };
        AddChild(_viewport);

        _park = new ProductionParkCanvas { Name = "RiversideProductionPark", YSortEnabled = true };
        _viewport.AddChild(_park);
        _park.Build();

        Node2D queueRings = new QueueRings { Name = "QueueRings", ZIndex = 0 };
        _park.AddChild(queueRings);
        _queueRoot = new Node2D { Name = "ReadableQueue", YSortEnabled = true, ZIndex = 1 };
        _park.AddChild(_queueRoot);

        Node2D stand = _park.FindChild("ZestStand", recursive: false, owned: false) as Node2D
            ?? throw new InvalidOperationException("ART-12 stand was not built.");
        _stockBoard = new PixelWorldText { Name = "StockBoard", Position = new Vector2(104, -55), ZIndex = 30 };
        _stockBoard.Configure("CLASSIC 12", ZestStyle.Palette.Cream, centered: true, background: new Color(ZestStyle.Palette.Charcoal, .88f));
        stand.AddChild(_stockBoard);

        _stars = new ReputationStars { Name = "ReputationStars", Position = new Vector2(104, -68), ZIndex = 31 };
        stand.AddChild(_stars);

        _queueBoard = new PixelWorldText { Name = "QueueBoard", Position = new Vector2(72, 18), ZIndex = 30 };
        _queueBoard.Configure("QUEUE 0", ZestStyle.Palette.Cream, centered: true, background: new Color(ZestStyle.Palette.Charcoal, .88f));
        _park.AddChild(_queueBoard);

        _focusMarker = new Line2D
        {
            Name = "CustomerFocus",
            Points = [new(-18, 0), new(0, 7), new(18, 0), new(0, -7), new(-18, 0)],
            Width = 2,
            DefaultColor = ZestStyle.Palette.ZestYellow,
            Closed = false,
            Visible = false,
            ZIndex = 20,
        };
        _park.AddChild(_focusMarker);

        int[] ambientStarts = [0, 3, 6, 9];
        string[] ambientSegments = ["commuter", "tourist", "student", "commuter"];
        for (int i = 0; i < ambientStarts.Length; i++)
        {
            HdCustomerActor walker = new() { Name = $"AmbientWalker_{i}" };
            walker.Configure(walksRoute: true, i, Guid.Empty, ambientStarts[i], segmentId: ambientSegments[i % ambientSegments.Length]);
            walker.SetSimulationSpeed(_simulationSpeed);
            walker.SetReducedMotion(_reducedMotion);
            walker.ZIndex = 1;
            _park.AddChild(walker);
            _ambientWalkers.Add(walker);
        }

        SetQueueLength(_requestedQueueLength);

        _camera = new Camera2D
        {
            Name = "PixelCamera",
            PositionSmoothingEnabled = false,
            Enabled = true,
        };
        _park.AddChild(_camera);

        _weatherTint = new ColorRect
        {
            Name = "WeatherTint",
            Color = new Color(0, 0, 0, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _weatherTint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _viewport.AddChild(_weatherTint);

        _eveningTint = new ColorRect
        {
            Name = "EveningTint",
            Color = new Color(0, 0, 0, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _eveningTint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _viewport.AddChild(_eveningTint);

        _rain = new RainOverlay
        {
            Name = "RainOverlay",
            Size = (Vector2)_viewport.Size,
        };
        _viewport.AddChild(_rain);
    }

    private void SelectAt(Vector2 localPoint)
    {
        if (_camera is null || Size.X <= 0 || Size.Y <= 0) return;
        Vector2 viewportPoint = localPoint * new Vector2(_viewport.Size.X / Size.X, _viewport.Size.Y / Size.Y);
        Vector2 worldPoint = (viewportPoint - (Vector2)_viewport.Size / 2f) / _camera.Zoom + _camera.Position;

        int customerIndex = -1;
        float nearest = float.MaxValue;
        for (int index = 0; index < _queuePeople.Count; index++)
        {
            float distance = worldPoint.DistanceTo(_queuePeople[index].Position + new Vector2(0, -10));
            if (distance >= nearest) continue;
            nearest = distance;
            customerIndex = index;
        }

        if (customerIndex >= 0 && nearest <= 23)
            WorldSelectionRequested?.Invoke(new(WorldSelectionKind.Customer, customerIndex));
        else if (new Rect2(-92, -118, 184, 144).HasPoint(worldPoint))
            WorldSelectionRequested?.Invoke(new(WorldSelectionKind.Stand));
        else
            WorldSelectionRequested?.Invoke(new(WorldSelectionKind.None));
    }

    /// <summary>Cheap falling-droplet overlay. Only draws when active.</summary>
    private partial class RainOverlay : Control
    {
        private const int DropCount = 28;
        private readonly Vector2[] _positions = new Vector2[DropCount];
        private readonly float[] _speeds = new float[DropCount];
        private readonly Color _color = new(0.72f, 0.82f, 1f, 0.55f);
        private bool _active;
        private RandomNumberGenerator _rng = new();

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            _rng.Seed = 0xDEA1;
            for (int i = 0; i < DropCount; i++) Respawn(i, initial: true);
            SetProcess(false);
            Visible = false;
        }

        public void SetActive(bool active)
        {
            if (_active == active) return;
            _active = active;
            Visible = active;
            SetProcess(active);
            if (active) QueueRedraw();
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;
            float h = Size.Y > 0 ? Size.Y : 360;
            for (int i = 0; i < DropCount; i++)
            {
                _positions[i].Y += _speeds[i] * dt;
                if (_positions[i].Y > h) Respawn(i, initial: false);
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (!_active) return;
            for (int i = 0; i < DropCount; i++)
            {
                Vector2 top = _positions[i];
                Vector2 bot = top + new Vector2(-2, 8);
                DrawLine(top, bot, _color, 1f);
            }
        }

        private void Respawn(int i, bool initial)
        {
            float w = Size.X > 0 ? Size.X : 640;
            float h = Size.Y > 0 ? Size.Y : 360;
            float x = _rng.RandfRange(0, w);
            float y = initial ? _rng.RandfRange(0, h) : _rng.RandfRange(-20, 0);
            _positions[i] = new Vector2(x, y);
            _speeds[i] = _rng.RandfRange(220, 340);
        }
    }

    /// <summary>Dashed cream rings on the ground marking each queue position — matches the reference visual.</summary>
    private partial class QueueRings : Node2D
    {
        public override void _Draw()
        {
            Color color = new(ZestStyle.Palette.Cream, 0.55f);
            foreach (Vector2 slot in ProductionParkCanvas.QueuePositions)
            {
                DrawDashedEllipse(slot + new Vector2(0, 2), 10, 4, color);
            }
        }

        private void DrawDashedEllipse(Vector2 center, float rx, float ry, Color color)
        {
            const int segments = 16;
            for (int i = 0; i < segments; i++)
            {
                if (i % 2 != 0) continue;
                float a0 = i * Mathf.Tau / segments;
                float a1 = (i + 1) * Mathf.Tau / segments;
                Vector2 p0 = center + new Vector2(Mathf.Cos(a0) * rx, Mathf.Sin(a0) * ry);
                Vector2 p1 = center + new Vector2(Mathf.Cos(a1) * rx, Mathf.Sin(a1) * ry);
                DrawLine(p0, p1, color, 1f);
            }
        }
    }

    /// <summary>Three-slot pixel star row rendered next to the stockboard.</summary>
    private partial class ReputationStars : Node2D
    {
        private int _count;
        private static readonly Color Filled = ZestStyle.Palette.ZestYellow;
        private static readonly Color Empty = new(1f, 1f, 1f, 0.18f);

        public void SetStarCount(int count)
        {
            count = Math.Clamp(count, 0, 3);
            if (_count == count) return;
            _count = count;
            QueueRedraw();
        }

        public override void _Draw()
        {
            const int spacing = 6;
            float originX = -(3 * spacing) / 2f + spacing / 2f;
            for (int i = 0; i < 3; i++)
            {
                Vector2 center = new(originX + i * spacing, 0);
                DrawStar(center, i < _count ? Filled : Empty);
            }
        }

        private void DrawStar(Vector2 c, Color color)
        {
            Vector2[] points =
            [
                c + new Vector2(0, -2),
                c + new Vector2(2, 0),
                c + new Vector2(0, 2),
                c + new Vector2(-2, 0),
            ];
            DrawColoredPolygon(points, color);
            DrawColoredPolygon(new Vector2[]
            {
                c + new Vector2(0, -3),
                c + new Vector2(1, -1),
                c + new Vector2(0, 0),
                c + new Vector2(-1, -1),
            }, color);
        }
    }

    private void ApplyCamera()
    {
        if (_camera is null) return;
        float zoom = _framing switch
        {
            CameraFraming.Neighborhood => 1f,
            CameraFraming.CloseObservation => 2f,
            _ => 1f,
        };
        _camera.Zoom = Vector2.One * zoom;
        _camera.Position = _target.Round();
    }
}
