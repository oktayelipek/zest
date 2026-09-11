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
    private HdCustomerActor _routeGuest = null!;
    private Line2D _focusMarker = null!;
    private PixelWorldText _stockBoard = null!;
    private PixelWorldText _queueBoard = null!;
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

    public void SetQueueCustomers(IReadOnlyList<Guid> customerIds, IReadOnlyDictionary<Guid, QueueDepartureKind>? departures = null)
    {
        ArgumentNullException.ThrowIfNull(customerIds);
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
                customer.Configure(walksRoute: false, index % 2, id);
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
        _routeGuest?.SetSimulationSpeed(_simulationSpeed);
    }

    public void SetReducedMotion(bool enabled)
    {
        _reducedMotion = enabled;
        foreach (HdCustomerActor actor in _queuePeople) actor.SetReducedMotion(enabled);
        _routeGuest?.SetReducedMotion(enabled);
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

        _queueRoot = new Node2D { Name = "ReadableQueue", YSortEnabled = true, ZIndex = 1 };
        _park.AddChild(_queueRoot);

        Node2D stand = _park.FindChild("ZestStand", recursive: false, owned: false) as Node2D
            ?? throw new InvalidOperationException("ART-12 stand was not built.");
        _stockBoard = new PixelWorldText { Name = "StockBoard", Position = new Vector2(104, -55), ZIndex = 30 };
        _stockBoard.Configure("CLASSIC 12", ZestStyle.Palette.Cream, centered: true, background: new Color(ZestStyle.Palette.Charcoal, .88f));
        stand.AddChild(_stockBoard);

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

        _routeGuest = new HdCustomerActor { Name = "PlayableRouteGuest" };
        _routeGuest.Configure(walksRoute: true, 0, Guid.Empty);
        _routeGuest.SetSimulationSpeed(_simulationSpeed);
        _routeGuest.SetReducedMotion(_reducedMotion);
        _routeGuest.ZIndex = 1;
        _park.AddChild(_routeGuest);

        SetQueueLength(_requestedQueueLength);

        _camera = new Camera2D
        {
            Name = "PixelCamera",
            PositionSmoothingEnabled = false,
            Enabled = true,
        };
        _park.AddChild(_camera);
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
