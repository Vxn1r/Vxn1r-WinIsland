namespace WinIsland.Services;

/// <summary>Small, side-effect-free helpers shared by the UI and tests.</summary>
public static class PercentMath
{
    public static int ClampPercent(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return 0;

        return (int)Math.Clamp(Math.Round(value, MidpointRounding.AwayFromZero), 0, 100);
    }

    public static float ToScalar(double value) => ClampPercent(value) / 100f;
}
