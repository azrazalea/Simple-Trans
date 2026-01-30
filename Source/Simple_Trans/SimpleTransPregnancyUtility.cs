using Verse;

namespace Simple_Trans;

/// <summary>
/// BACKWARDS COMPATIBILITY SHIM
///
/// This class exists solely for backwards compatibility with mods that depend on Simple Trans,
/// such as "The topic of Gender" (GenderAcceptance).
///
/// When we refactored the code, we moved these definitions to SimpleTransHediffs.
/// This shim provides the same field names so dependent mods don't break.
///
/// DO NOT add new functionality here - use SimpleTransHediffs instead.
/// </summary>
public static class SimpleTransPregnancyUtility
{
	/// <summary>
	/// Cisgender hediff definition
	/// </summary>
	public static readonly HediffDef cisDef = HediffDef.Named("Cisgender");

	/// <summary>
	/// Transgender hediff definition
	/// </summary>
	public static readonly HediffDef transDef = HediffDef.Named("Transgender");

	/// <summary>
	/// Pregnancy carry capability hediff definition
	/// </summary>
	public static readonly HediffDef canCarryDef = HediffDef.Named("PregnancyCarry");

	/// <summary>
	/// Pregnancy sire capability hediff definition
	/// </summary>
	public static readonly HediffDef canSireDef = HediffDef.Named("PregnancySire");
}
