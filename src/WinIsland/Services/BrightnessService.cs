using System.Management;

namespace WinIsland.Services;

/// <summary>
/// Uses the Windows WMI monitor-brightness interface. Most built-in laptop panels
/// expose it; some external monitors and graphics drivers do not.
/// </summary>
public static class BrightnessService
{
    public static int? ReadPercent()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\WMI", "SELECT * FROM WmiMonitorBrightness");
            using var results = searcher.Get();
            foreach (ManagementObject item in results)
            {
                using (item)
                    return PercentMath.ClampPercent(Convert.ToDouble(item["CurrentBrightness"]));
            }
        }
        catch
        {
            // The UI shows an unavailable state instead of failing the app.
        }
        return null;
    }

    public static bool TrySetPercent(int percent)
    {
        try
        {
            var target = (byte)PercentMath.ClampPercent(percent);
            using var searcher = new ManagementObjectSearcher(
                @"root\WMI", "SELECT * FROM WmiMonitorBrightnessMethods");
            using var results = searcher.Get();
            var found = false;
            foreach (ManagementObject item in results)
            {
                using (item)
                {
                    item.InvokeMethod("WmiSetBrightness", new object[] { 1u, target });
                    found = true;
                }
            }
            return found;
        }
        catch
        {
            return false;
        }
    }
}
