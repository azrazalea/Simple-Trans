using RimWorld;
using Verse;

namespace Simple_Trans;

/// <summary>
/// Hediff definitions and query methods for Simple Trans mod
/// Contains static HediffDef references and capability check methods
/// </summary>
public static class SimpleTransHediffs
{
	#region Hediff Definitions

	/// <summary>
	/// Hediff definition for cisgender identity
	/// </summary>
	public static readonly HediffDef cisDef = HediffDef.Named("Cisgender");

	/// <summary>
	/// Hediff definition for transgender identity
	/// </summary>
	public static readonly HediffDef transDef = HediffDef.Named("Transgender");

	/// <summary>
	/// Hediff definition for pregnancy carrying ability
	/// </summary>
	public static readonly HediffDef canCarryDef = HediffDef.Named("PregnancyCarry");

	/// <summary>
	/// Hediff definition for pregnancy siring ability
	/// </summary>
	public static readonly HediffDef canSireDef = HediffDef.Named("PregnancySire");

	#endregion

	#region Public Query Methods

	/// <summary>
	/// Determines if a pawn can carry pregnancies
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>True if the pawn can carry pregnancies</returns>
	public static bool CanCarry(Pawn pawn)
	{
		return CanCarryReport(pawn).Accepted;
	}

	/// <summary>
	/// Determines if a pawn can carry pregnancies with detailed reason
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>AcceptanceReport with reason if unable to carry</returns>
	public static AcceptanceReport CanCarryReport(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null)
		{
			SimpleTransDebug.Log("CanCarryReport called with null pawn or missing health data", 1);
			return "CannotNoAbility".Translate();
		}

		// Check if pawn has carry capability
		if (!pawn.health.hediffSet.HasHediff(canCarryDef, false))
		{
			return "CannotNoAbility".Translate();
		}

		// Check for sterilization that blocks carrying
		if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Sterilized) != null)
		{
			return "CannotSterile".Translate();
		}

		var sterilizedCarryDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedCarry");
		if (sterilizedCarryDef != null && pawn.health.hediffSet.HasHediff(sterilizedCarryDef, false))
		{
			return "CannotSterile".Translate();
		}

		var reversibleSterilizedCarryDef = DefDatabase<HediffDef>.GetNamedSilentFail("ReversibleSterilizedCarry");
		if (reversibleSterilizedCarryDef != null && pawn.health.hediffSet.HasHediff(reversibleSterilizedCarryDef, false))
		{
			return "CannotSterile".Translate();
		}

		return true;
	}

	/// <summary>
	/// Determines if a pawn can sire offspring
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>True if the pawn can sire offspring</returns>
	public static bool CanSire(Pawn pawn)
	{
		return CanSireReport(pawn).Accepted;
	}

	/// <summary>
	/// Determines if a pawn can sire offspring with detailed reason
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>AcceptanceReport with reason if unable to sire</returns>
	public static AcceptanceReport CanSireReport(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null)
		{
			SimpleTransDebug.Log("CanSireReport called with null pawn or missing health data", 1);
			return "CannotNoAbility".Translate();
		}

		// Check if pawn has sire capability
		if (!pawn.health.hediffSet.HasHediff(canSireDef, false))
		{
			return "CannotNoAbility".Translate();
		}

		// Check for sterilization that blocks siring
		if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Sterilized) != null)
		{
			return "CannotSterile".Translate();
		}

		var sterilizedSireDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedSire");
		if (sterilizedSireDef != null && pawn.health.hediffSet.HasHediff(sterilizedSireDef, false))
		{
			return "CannotSterile".Translate();
		}

		var reversibleSterilizedSireDef = DefDatabase<HediffDef>.GetNamedSilentFail("ReversibleSterilizedSire");
		if (reversibleSterilizedSireDef != null && pawn.health.hediffSet.HasHediff(reversibleSterilizedSireDef, false))
		{
			return "CannotSterile".Translate();
		}

		return true;
	}

	/// <summary>
	/// Determines if a pawn is cisgender (has the Cisgender hediff)
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>True if the pawn is cisgender</returns>
	public static bool IsCisgender(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null)
		{
			return false;
		}

		return pawn.health.hediffSet.HasHediff(cisDef, false);
	}

	/// <summary>
	/// Checks if a pawn is non-binary
	/// Default implementation returns false, but NBG mod patches this to return EnbyUtility.IsEnby(pawn)
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>True if the pawn is non-binary, false otherwise</returns>
	public static bool IsEnby(Pawn pawn)
	{
		return false; // Default implementation - our NBGPatches patches this
	}

	#endregion
}
