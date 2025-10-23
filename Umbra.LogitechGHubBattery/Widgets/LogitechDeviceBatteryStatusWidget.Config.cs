using System.Collections.Generic;
using Umbra.Widgets;

namespace Umbra.LogitechGHubBattery.Widgets;

internal partial class LogitechDeviceBatteryStatusWidget
{
    private static Dictionary<string, string> AllEntries { get; set; } = [];

    protected override IEnumerable<IWidgetConfigVariable> GetConfigVariables()
    {
        if (AllEntries.Count == 0) {
            AllEntries = new() { { "", "" } };

            foreach (var entry in _logitechHub.GetDeviceInfos()) {
                AllEntries[entry.Id] = entry.DisplayName;
            }
        }
        
        return [
            ..base.GetConfigVariables(),
            new SelectWidgetConfigVariable(
                "SelectedDevice",
                "Logitech device",
                "Logitech device to show the battery status for",
                "",
                AllEntries
            ),
        ];
    }
}