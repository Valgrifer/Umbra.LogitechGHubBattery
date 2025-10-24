using Dalamud.Interface;
using System;
using System.Linq;
using Umbra.Common;
using Umbra.LogitechGHubBattery.Services;
using Una.Drawing;

namespace Umbra.LogitechGHubBattery;

internal static class LogitechNodeUtils
{
    internal static UdtDocument DocumentFrom(string resourceName)
    {
        foreach (var assembly in Framework.Assemblies) {
            var resource = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));
            
            if (resource == null) continue;
            
            return UdtLoader.LoadFromAssembly(assembly, resourceName);
        }
        
        throw new Exception($"No UDT document with the name \"{resourceName}\" exists in any assembly.");
    }
    
    internal static FontAwesomeIcon GetMainIcon(DeviceInfo info)
    {
        return info.DeviceType switch
        {
            "MOUSE" => FontAwesomeIcon.Mouse,
            "KEYBOARD" => FontAwesomeIcon.Keyboard,
            "HEADSET" => FontAwesomeIcon.Headset,
            _ => FontAwesomeIcon.Plug
        };
    }
    
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