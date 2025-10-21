namespace MarketScanner.Utilities;

/// <summary>
/// Utility class for volume-related calculations.
/// </summary>
public static class VolumeCalculations
{
    /// <summary>
    /// Calculates relative volume (current volume / average volume).
    /// Returns 0 if either value is invalid.
    /// </summary>
    /// <param name="currentVolume">Current trading volume</param>
    /// <param name="averageVolume">Average trading volume over a period</param>
    /// <returns>Relative volume ratio, or 0 if calculation is not possible</returns>
    public static double CalculateRelativeVolume(long currentVolume, long averageVolume)
    {
        if (currentVolume <= 0 || averageVolume <= 0)
            return 0.0;
        
        return (double)currentVolume / averageVolume;
    }
}
