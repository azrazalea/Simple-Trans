using RimWorld;

namespace Simple_Trans;

/// <summary>
/// Defines constants used throughout the Simple Trans mod to replace magic numbers
/// </summary>
public static class SimpleTransConstants
{
    /// <summary>
    /// Default rate for cisgender identity (90%)
    /// </summary>
    public const float DefaultCisRate = 0.9f;
    
    /// <summary>
    /// Default rate for male carrying pregnancy (100%)
    /// </summary>
    public const float DefaultMaleCarryRate = 1.0f;
    
    /// <summary>
    /// Default rate for female siring (100%)
    /// </summary>
    public const float DefaultFemaleSireRate = 1.0f;
    
    /// <summary>
    /// Default rate for non-binary carrying pregnancy (50%)
    /// </summary>
    public const float DefaultNonBinaryCarryRate = 0.5f;
    
    /// <summary>
    /// Minimum age for reproductive biology (16 years)
    /// </summary>
    public const int MinimumReproductiveAge = 16;
    
    /// <summary>
    /// Random chance for having no father relationship (20%)
    /// </summary>
    public const float RandomFatherlessChance = 0.2f;
    
    /// <summary>
    /// Percentage conversion factor
    /// </summary>
    public const float PercentageToDecimal = 100f;
}