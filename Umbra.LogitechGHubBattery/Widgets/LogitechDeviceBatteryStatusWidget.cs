using Dalamud.Interface;
using System.Collections.Generic;
using System.Linq;
using Umbra.Common;
using Umbra.LogitechGHubBattery.Services;
using Umbra.Widgets;
using Una.Drawing;

namespace Umbra.LogitechGHubBattery.Widgets;

[ToolbarWidget(
    "LogitechDeviceBatteryStatus",
    "Logitech Device Battery Status",
    "This is a sample widget from the Umbra.LogitechGHubBattery repository."
)]
internal partial class LogitechDeviceBatteryStatusWidget(
    WidgetInfo                  info,
    string?                     guid         = null,
    Dictionary<string, object>? configValues = null
) : StandardToolbarWidget(info, guid, configValues)
{
    protected override StandardWidgetFeatures Features =>
        StandardWidgetFeatures.Text |
        StandardWidgetFeatures.Icon |
        StandardWidgetFeatures.CustomizableIcon;

    public override WidgetPopup? Popup => null;

    protected override bool DefaultDecorate => true;
    protected override string DefaultIconType => IconTypeFontAwesome;
    protected override uint DefaultGameIconId => (uint) FontAwesomeIcon.Plug;
    
    private readonly LogitechHub _logitechHub = Framework.Service<LogitechHub>();
    private string? DeviceId => string.IsNullOrEmpty(GetConfigValue<string>("SelectedDevice")) ? null : GetConfigValue<string>("SelectedDevice");
    private DeviceInfo? Device => DeviceId == null ? null : _logitechHub.GetDeviceInfos().FirstOrDefault(d => d.Id == DeviceId);
    
    
    private readonly Node _chargingIcon = new()
    {
        ClassList = ["icon", "fa-icon"],
        Style = new ()
        {
            Anchor = Anchor.TopRight
        }
    };

    public override string GetInstanceName()
    {
        var device = Device;
        return device != null ? $"Battery Status - {device.DisplayName}" : "Battery Status";
    }

    protected override void OnLoad()
    {
        IsVisible = false;
        
        BodyNode.AppendChild(_chargingIcon);
        BodyNode.Style.AutoSize = new(AutoSize.Grow, AutoSize.Grow);
        IconNode.Style.Anchor = Anchor.TopLeft;
        Node.QuerySelector(".body")!.Style.Anchor = Anchor.TopCenter;
    }

    protected override void OnDraw()
    {
        var device = DeviceId;
        
        if (device == null)
        {
            IsVisible = false;
            return;
        }
        
        var battery = _logitechHub.GetBatteryState(device);
        
        if (battery == null)
        {
            IsVisible = false;
            return;
        }
        
        IsVisible = true;
        SetText($"{battery.Percentage:0} %");

        SetSecondIcon(battery);
    }

    private void SetSecondIcon(BatteryState state)
    {
        _chargingIcon.NodeValue = LogitechNodeUtils.GetSecondIcon(state).ToIconString();
    }
}
