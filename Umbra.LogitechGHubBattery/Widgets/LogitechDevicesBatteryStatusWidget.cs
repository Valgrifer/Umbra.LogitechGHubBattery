using Dalamud.Interface;
using System.Collections.Generic;
using Umbra.Common;
using Umbra.LogitechGHubBattery.Services;
using Umbra.Widgets;
using Una.Drawing;

namespace Umbra.LogitechGHubBattery.Widgets;

[ToolbarWidget(
    "LogitechDevicesBatteryStatus",
    "Logitech Device List Battery Status",
    "This is a sample widget from the Umbra.LogitechGHubBattery repository."
)]
internal class LogitechDevicesBatteryStatusWidget(
    WidgetInfo                  info,
    string?                     guid         = null,
    Dictionary<string, object>? configValues = null
) : ToolbarWidget(info, guid, configValues)
{
    public override WidgetPopup? Popup => null;
    
    public override Node Node { get; } = new () {
        Style = new () {
            Gap = 8,
        }
    };
    
    private const string CvarNameDecorate = "Decorate";
    private bool         CvarDecorate()  => GetConfigValue<bool>(CvarNameDecorate);

    private readonly LogitechHub _logitechHub = Framework.Service<LogitechHub>();

    protected override void Initialize()
    {
    }
    
    protected override IEnumerable<IWidgetConfigVariable> GetConfigVariables()
    {
        return [
            new BooleanWidgetConfigVariable(
            CvarNameDecorate,
            I18N.Translate("Widgets.Standard.Config.Decorate.Name"),
            I18N.Translate("Widgets.Standard.Config.Decorate.Description"),
            true
            ) { Category = I18N.Translate("Widgets.Standard.Config.Category.General") }
        ];
    }
    
    protected override void OnUpdate()
    {
        var isVertical = IsMemberOfVerticalBar;
        Node.Style.Flow = isVertical ? Flow.Vertical : Flow.Horizontal;

        var list = _logitechHub.GetDeviceInfos();
        
        foreach (var device in list)
        {
            Node? node = Node.QuerySelector($"#{device.Id}");

            if (node == null)
            {
                node = LogitechNodeUtils.DocumentFrom("logitech._standard.xml").RootNode!;
                node.Id = device.Id;
                
                    node.Style.Size     = new(0, SafeHeight);
                if (!isVertical)
                    node.Style.AutoSize = (AutoSize.Fit, AutoSize.Grow);
                
                node.QuerySelector("#icon")!.NodeValue = LogitechNodeUtils.GetMainIcon(device).ToIconString();
                
                Node.AppendChild(node);
            }
            
            node.ToggleClass("decorated", CvarDecorate());
            
            var text = node.QuerySelector(".text")!;
            var second = node.QuerySelector("#state")!;
            
            var battery = _logitechHub.GetBatteryState(device.Id);
            
            if (battery == null)
            {
                node.Style.IsVisible = false;
                return;
            }
            
            node.Style.IsVisible = true;
            text.NodeValue = $"{battery.Percentage:0} %";

            second.NodeValue = LogitechNodeUtils.GetSecondIcon(battery).ToIconString();
        }

        foreach (var node in Node.ChildNodes)
        {
            if (list.FindIndex(device => node.Id == device.Id) != -1) continue;
            node.Style.IsVisible = false;
        }
    }
}
