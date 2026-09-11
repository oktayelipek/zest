using Godot;

namespace Zest.Game;

public partial class PreviewCapture : Node
{
    private int _frames;
    private bool _attempted;
    private bool _framingApplied;
    private string _mode = "business";
    private Main _main = null!;

    public override void _Ready()
    {
        string[] arguments = OS.GetCmdlineArgs();
        string environmentMode = OS.GetEnvironment("ZEST_CAPTURE_MODE");
        if (!string.IsNullOrWhiteSpace(environmentMode)) _mode = environmentMode;
        if (arguments.Contains("neighborhood", StringComparer.OrdinalIgnoreCase)) _mode = "neighborhood";
        if (arguments.Contains("close", StringComparer.OrdinalIgnoreCase)) _mode = "close";
        if (arguments.Contains("style", StringComparer.OrdinalIgnoreCase)) _mode = "style";
        if (arguments.Contains("world-first", StringComparer.OrdinalIgnoreCase)) _mode = "world-first";
        if (arguments.Any(argument => argument.TrimStart('-').Equals("art12", StringComparison.OrdinalIgnoreCase))) _mode = "art12";
        if (arguments.Contains("stand", StringComparer.OrdinalIgnoreCase)) _mode = "stand";
        if (arguments.Contains("customer", StringComparer.OrdinalIgnoreCase)) _mode = "customer";
        if (arguments.Contains("notebook", StringComparer.OrdinalIgnoreCase)) _mode = "notebook";
        if (arguments.Contains("inventory", StringComparer.OrdinalIgnoreCase)) _mode = "inventory";
        if (arguments.Contains("paused-product", StringComparer.OrdinalIgnoreCase)) _mode = "paused-product";
        if (arguments.Contains("closing-hour", StringComparer.OrdinalIgnoreCase)) _mode = "closing-hour";
        if (arguments.Contains("time-controls", StringComparer.OrdinalIgnoreCase)) _mode = "time-controls";
        if (arguments.Contains("emergency-restock", StringComparer.OrdinalIgnoreCase)) _mode = "emergency-restock";
        if (arguments.Contains("diagnostics", StringComparer.OrdinalIgnoreCase)) _mode = "diagnostics";
        if (arguments.Contains("art04-counter", StringComparer.OrdinalIgnoreCase)) _mode = "art04-counter";
        if (arguments.Contains("art04-juicer", StringComparer.OrdinalIgnoreCase)) _mode = "art04-juicer";
        if (arguments.Contains("art04-cooler", StringComparer.OrdinalIgnoreCase)) _mode = "art04-cooler";
        if (arguments.Any(argument => argument.TrimStart('-').Equals("p0-layered", StringComparison.OrdinalIgnoreCase))) _mode = "p0-layered";
        _main = new Main { StartInLiveStudy = true };
        AddChild(_main);
        _main.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    public override void _Process(double delta)
    {
        _frames++;
        if (!_framingApplied && _frames >= 4)
        {
            ParkWorldView? world = FindChild("ParkWorldView", recursive: true, owned: false) as ParkWorldView;
            if (world is not null)
            {
                if (_mode == "neighborhood") world.SetFraming(ParkWorldView.CameraFraming.Neighborhood);
                if (_mode == "close") world.TrackQueueCustomer(0);
                if (_mode == "stand") _main.PreviewOpenStandContext();
                if (_mode == "customer") _main.PreviewOpenCustomerContext();
                if (_mode == "notebook") _main.PreviewOpenNotebook();
                if (_mode == "inventory") _main.PreviewOpenInventory();
                if (_mode == "paused-product") _main.PreviewPauseClassic();
                if (_mode == "closing-hour") _main.PreviewClosingHour();
                if (_mode == "emergency-restock") _main.PreviewEmergencyRestock();
                if (_mode == "diagnostics") _main.PreviewOpenDiagnostics();
                if (_mode == "art04-counter") _main.PreviewArt04(StandUpgradeVisual.BetterCounter, StandOperatingVisual.Normal);
                if (_mode == "art04-juicer") _main.PreviewArt04(StandUpgradeVisual.ElectricJuicer, StandOperatingVisual.RushMenu);
                if (_mode == "art04-cooler") _main.PreviewArt04(StandUpgradeVisual.BiggerCooler, StandOperatingVisual.SoldOut);
                if (_mode == "p0-layered") world.SetFraming(ParkWorldView.CameraFraming.CloseObservation);
                if (_mode.StartsWith("vendor-", StringComparison.Ordinal))
                {
                    world.SetFraming(ParkWorldView.CameraFraming.CloseObservation);
                    world.SetVendorPose(_mode switch
                    {
                        "vendor-prepare" => VendorPose.Prepare,
                        "vendor-pour" => VendorPose.Pour,
                        "vendor-handoff" => VendorPose.Handoff,
                        _ => VendorPose.Idle,
                    });
                }
                _framingApplied = true;
            }
        }
        if (_mode.StartsWith("vendor-", StringComparison.Ordinal))
        {
            ParkWorldView? previewWorld = FindChild("ParkWorldView", recursive: true, owned: false) as ParkWorldView;
            previewWorld?.SetVendorPose(_mode switch
            {
                "vendor-prepare" => VendorPose.Prepare,
                "vendor-pour" => VendorPose.Pour,
                "vendor-handoff" => VendorPose.Handoff,
                _ => VendorPose.Idle,
            });
        }
        int captureFrame = _mode == "art12" ? 40 : 10;
        if (_attempted || !_framingApplied || _frames < captureFrame) return;
        _attempted = true;
        string fileName = _mode switch
        {
            "style" => "art-02-visual-target.png",
            "world-first" => "ui-12-world-first.png",
            "art12" => "art-12-first-playable-pixel-world.png",
            "stand" => "ui-04-stand-context.png",
            "customer" => "ui-03-customer-context.png",
            "notebook" => "ui-03-management-layer.png",
            "inventory" => "ui-04-inventory-details.png",
            "paused-product" => "ui-04-paused-world-sync.png",
            "closing-hour" => "ui-08-closing-hour.png",
            "time-controls" => "ui-08-time-controls.png",
            "emergency-restock" => "e08-03-emergency-restock.png",
            "diagnostics" => "e08-04-active-diagnostics.png",
            "art04-counter" => "art-04-better-counter.png",
            "art04-juicer" => "art-04-electric-juicer-rush-menu.png",
            "art04-cooler" => "art-04-bigger-cooler-sold-out.png",
            "p0-layered" => "p0-layered-stand-vendor-1080p.png",
            "vendor-prepare" => "vendor-prepare-in-world-1080p.png",
            "vendor-pour" => "vendor-pour-in-world-1080p.png",
            "vendor-handoff" => "vendor-handoff-in-world-1080p.png",
            _ => $"art-01-{_mode}.png",
        };
        string output = ProjectSettings.GlobalizePath($"res://../docs/artifacts/{fileName}");
        Image? image = GetViewport().GetTexture().GetImage();
        if (image is null)
        {
            GD.PushError("Visual target preview capture has no render texture.");
            GetTree().Quit(1);
            return;
        }
        Error result = image.SavePng(output);
        GD.Print($"Visual target preview capture: {result} -> {output}");
        GetTree().Quit(result == Error.Ok ? 0 : 1);
    }
}
