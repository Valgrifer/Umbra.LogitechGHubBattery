using Dalamud.Interface;
using Umbra.LogitechGHubBattery.Services;

namespace Umbra.LogitechGHubBattery;

internal sealed class LogitechNodeUtils
{
    internal static FontAwesomeIcon GetSecondIcon(BatteryState state)
    {
        if (state.FullyCharged)
            return FontAwesomeIcon.BatteryFull;
        if (state.Charging)
            return FontAwesomeIcon.PlugCircleBolt;
        return state.Percentage switch
        {
            > 80 => FontAwesomeIcon.BatteryFull,
            > 60 => FontAwesomeIcon.BatteryThreeQuarters,
            > 40 => FontAwesomeIcon.BatteryHalf,
            > 20 => FontAwesomeIcon.BatteryQuarter,
            _ => FontAwesomeIcon.BatteryEmpty
        };
    }
}