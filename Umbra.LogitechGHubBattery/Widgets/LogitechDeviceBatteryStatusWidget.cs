using Dalamud.Interface;
using System.Collections.Generic;
using System.Linq;
using Umbra.Common;
using Umbra.LogitechGHubBattery.Services;
using Umbra.Widgets;

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

    public override string GetInstanceName()
    {
        var device = Device;
        return device != null ? $"Battery Status - {device.DisplayName}" : "Battery Status";
    }

    protected override void OnLoad()
    {
        IsVisible = false;
        SetText("---");
    }

    protected override void OnDraw()
    {
        var device = DeviceId;
        
        if (device == null)
        {
            IsVisible = false;
            return;
        }
        
        var battery = _logitechHub.GetBatteryState(device)?.Percentage.ToString("0");
        
        if (battery == null)
        {
            IsVisible = false;
            return;
        }
        
        IsVisible = true;
        SetText($"{battery} %");
    }
}
