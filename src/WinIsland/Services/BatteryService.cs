using System.Windows.Forms;

namespace WinIsland.Services;

public sealed record BatterySnapshot(int? Percent, string PowerSource, bool IsCharging);

public static class BatteryService
{
    public static BatterySnapshot Read()
    {
        try
        {
            var status = SystemInformation.PowerStatus;
            var fraction = status.BatteryLifePercent;
            int? percent = float.IsFinite(fraction) && fraction >= 0 && fraction <= 1
                ? PercentMath.ClampPercent(fraction * 100)
                : null;

            var source = status.PowerLineStatus switch
            {
                PowerLineStatus.Online => "Plugged in",
                PowerLineStatus.Offline => "On battery",
                _ => "Power source unknown"
            };

            var charging = status.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Charging);
            return new BatterySnapshot(percent, source, charging);
        }
        catch
        {
            return new BatterySnapshot(null, "Battery status unavailable", false);
        }
    }
}
