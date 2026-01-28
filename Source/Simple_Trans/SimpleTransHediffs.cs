using RimWorld;
using Verse;

namespace Simple_Trans;

/// <summary>
/// Hediff definitions and query methods for Simple Trans mod
/// Contains static HediffDef references and capability check methods
/// </summary>
public static class SimpleTransHediffs
{
	#region Age Fertility Curves

	/// <summary>
	/// Siring fertility age curve - gradual decline into old age
	/// Fertile from 18, stable until 50, gradual decline to 90
	/// </summary>
	private static readonly SimpleCurve SireFertilityAgeCurve = new SimpleCurve
	{
		new CurvePoint(14f, 0f),
		new CurvePoint(18f, 1f),
		new CurvePoint(50f, 1f),
		new CurvePoint(90f, 0f)
	};

	/// <summary>
	/// Carrying fertility age curve - includes natural decline/menopause
	/// Peaks at 20-28, declines sharply after 35, ends at 50
	/// </summary>
	private static readonly SimpleCurve CarryFertilityAgeCurve = new SimpleCurve
	{
		new CurvePoint(14f, 0f),
		new CurvePoint(20f, 1f),
		new CurvePoint(28f, 1f),
		new CurvePoint(35f, 0.5f),
		new CurvePoint(40f, 0.1f),
		new CurvePoint(45f, 0.02f),
		new CurvePoint(50f, 0f)
	};

	/// <summary>
	/// Gets the age factor for carrying capability.
	/// Prosthetics and Integrated Implants wombs don't age (return 1.0), natural organs use carry curve.
	/// </summary>
	public static float GetCarryAgeFactor(Pawn pawn)
	{
		if (pawn?.ageTracker == null)
			return 1f;

		// Prosthetics and Integrated Implants wombs don't age
		if (HasBionicCarry(pawn) ||
			pawn.health?.hediffSet?.HasHediff(HediffDef.Named("BasicProstheticCarry")) == true ||
			HasIntegratedImplantsWomb(pawn))
		{
			return 1f;
		}

		return CarryFertilityAgeCurve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
	}

	/// <summary>
	/// Gets the age factor for siring capability.
	/// Prosthetics don't age (return 1.0), natural organs use sire curve.
	/// </summary>
	public static float GetSireAgeFactor(Pawn pawn)
	{
		if (pawn?.ageTracker == null)
			return 1f;

		// Prosthetics don't age
		if (HasBionicSire(pawn) ||
			pawn.health?.hediffSet?.HasHediff(HediffDef.Named("BasicProstheticSire")) == true)
		{
			return 1f;
		}

		return SireFertilityAgeCurve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
	}

	#endregion

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

		// Check if pawn has carry capability (base hediff OR prosthetic alternatives OR Integrated Implants wombs)
		bool hasCarryCapability = pawn.health.hediffSet.HasHediff(canCarryDef, false)
			|| HasBionicCarry(pawn)
			|| pawn.health.hediffSet.HasHediff(HediffDef.Named("BasicProstheticCarry"))
			|| HasIntegratedImplantsWomb(pawn);

		if (!hasCarryCapability)
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

		// Integrated Implants - MechanicalBirthControl acts as sterilization
		var mechanicalBirthControlDef = DefDatabase<HediffDef>.GetNamedSilentFail("MechanicalBirthControl");
		if (mechanicalBirthControlDef != null && pawn.health.hediffSet.HasHediff(mechanicalBirthControlDef, false))
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

		// Check if pawn has sire capability (base hediff OR prosthetic alternatives)
		bool hasSireCapability = pawn.health.hediffSet.HasHediff(canSireDef, false)
			|| HasBionicSire(pawn)
			|| pawn.health.hediffSet.HasHediff(HediffDef.Named("BasicProstheticSire"));

		if (!hasSireCapability)
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

	#region Capability-Based Fertility

	/// <summary>
	/// Gets fertility for carrying capability using the appropriate age curve
	/// Prosthetic organs don't age - they use fixed fertility values
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>Fertility value for carrying (0.0 to 1.0+)</returns>
	public static float GetCarryFertility(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null || pawn?.ageTracker == null)
		{
			return 0f;
		}

		// Check if pawn can carry at all
		if (!CanCarry(pawn))
		{
			return 0f;
		}

		// Integrated Implants wombs - use their fertility values (superior to prosthetics)
		var archowombDef = DefDatabase<HediffDef>.GetNamedSilentFail("Archowomb");
		if (archowombDef != null && pawn.health.hediffSet.HasHediff(archowombDef, false))
		{
			SimpleTransDebug.Log($"Carry fertility for {pawn.Name?.ToStringShort}: 2.0 (archowomb - no aging)", 3);
			return 2.0f;
		}
		var synthwombDef = DefDatabase<HediffDef>.GetNamedSilentFail("Synthwomb");
		if (synthwombDef != null && pawn.health.hediffSet.HasHediff(synthwombDef, false))
		{
			SimpleTransDebug.Log($"Carry fertility for {pawn.Name?.ToStringShort}: 1.5 (synthwomb - no aging)", 3);
			return 1.5f;
		}

		// Prosthetics don't age - return fixed fertility values
		if (HasBionicCarry(pawn))
		{
			SimpleTransDebug.Log($"Carry fertility for {pawn.Name?.ToStringShort}: 1.2 (bionic - no aging)", 3);
			return 1.2f;
		}
		if (pawn.health.hediffSet.HasHediff(HediffDef.Named("BasicProstheticCarry")))
		{
			SimpleTransDebug.Log($"Carry fertility for {pawn.Name?.ToStringShort}: 0.7 (basic prosthetic - no aging)", 3);
			return 0.7f;
		}

		// Natural organs - apply age curve
		float baseFertility = GetBaseFertilityWithoutAge(pawn);
		float ageFactor = GetCarryAgeFactor(pawn);
		float fertility = baseFertility * ageFactor;

		SimpleTransDebug.Log($"Carry fertility for {pawn.Name?.ToStringShort}: {fertility:F3} (base: {baseFertility:F3}, age factor: {ageFactor:F3})", 3);

		return fertility;
	}

	/// <summary>
	/// Gets fertility for siring capability using the appropriate age curve
	/// Prosthetic organs don't age - they use fixed fertility values
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>Fertility value for siring (0.0 to 1.0+)</returns>
	public static float GetSireFertility(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null || pawn?.ageTracker == null)
		{
			return 0f;
		}

		// Check if pawn can sire at all
		if (!CanSire(pawn))
		{
			return 0f;
		}

		// Prosthetics don't age - return fixed fertility values
		if (HasBionicSire(pawn))
		{
			SimpleTransDebug.Log($"Sire fertility for {pawn.Name?.ToStringShort}: 1.2 (bionic - no aging)", 3);
			return 1.2f;
		}
		if (pawn.health.hediffSet.HasHediff(HediffDef.Named("BasicProstheticSire")))
		{
			SimpleTransDebug.Log($"Sire fertility for {pawn.Name?.ToStringShort}: 0.7 (basic prosthetic - no aging)", 3);
			return 0.7f;
		}

		// Natural organs - apply age curve
		float baseFertility = GetBaseFertilityWithoutAge(pawn);
		float ageFactor = GetSireAgeFactor(pawn);
		float fertility = baseFertility * ageFactor;

		SimpleTransDebug.Log($"Sire fertility for {pawn.Name?.ToStringShort}: {fertility:F3} (base: {baseFertility:F3}, age factor: {ageFactor:F3})", 3);

		return fertility;
	}

	/// <summary>
	/// Checks if pawn has bionic carrying prosthetic
	/// </summary>
	public static bool HasBionicCarry(Pawn pawn)
	{
		return pawn?.health?.hediffSet?.HasHediff(HediffDef.Named("BionicProstheticCarry")) == true;
	}

	/// <summary>
	/// Checks if pawn has bionic siring prosthetic
	/// </summary>
	public static bool HasBionicSire(Pawn pawn)
	{
		return pawn?.health?.hediffSet?.HasHediff(HediffDef.Named("BionicProstheticSire")) == true;
	}

	/// <summary>
	/// Checks if pawn has Integrated Implants womb (Synthwomb or Archowomb)
	/// </summary>
	public static bool HasIntegratedImplantsWomb(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null)
			return false;

		var synthwombDef = DefDatabase<HediffDef>.GetNamedSilentFail("Synthwomb");
		var archowombDef = DefDatabase<HediffDef>.GetNamedSilentFail("Archowomb");

		return (synthwombDef != null && pawn.health.hediffSet.HasHediff(synthwombDef, false)) ||
			   (archowombDef != null && pawn.health.hediffSet.HasHediff(archowombDef, false));
	}

	#endregion

	#region Role Assignment

	/// <summary>
	/// Determines reproductive roles (carrier/sirer) for two pawns.
	/// Uses consistent logic across all pregnancy-related code.
	/// When both pawns can fill both roles, assigns different pawns to each role.
	/// </summary>
	/// <param name="pawn1">First pawn</param>
	/// <param name="pawn2">Second pawn</param>
	/// <param name="carrier">Output: pawn who will carry, or null if none can</param>
	/// <param name="sirer">Output: pawn who will sire, or null if none can</param>
	public static void DetermineRoles(Pawn pawn1, Pawn pawn2, out Pawn carrier, out Pawn sirer)
	{
		carrier = null;
		sirer = null;

		if (pawn1 == null && pawn2 == null)
			return;

		bool p1CanCarry = CanCarry(pawn1);
		bool p1CanSire = CanSire(pawn1);
		bool p2CanCarry = CanCarry(pawn2);
		bool p2CanSire = CanSire(pawn2);

		// Case 1: Clear role separation (one carries, other sires)
		if (p1CanCarry && !p1CanSire && p2CanSire && !p2CanCarry)
		{
			carrier = pawn1;
			sirer = pawn2;
			return;
		}
		if (p2CanCarry && !p2CanSire && p1CanSire && !p1CanCarry)
		{
			carrier = pawn2;
			sirer = pawn1;
			return;
		}

		// Case 2: One pawn has both, other has one or none
		// Assign the exclusive role first, then give remaining role to dual-capable pawn
		if (p1CanCarry && p1CanSire)
		{
			// pawn1 can do both
			if (p2CanSire && !p2CanCarry)
			{
				// pawn2 can only sire, so pawn1 carries
				carrier = pawn1;
				sirer = pawn2;
				return;
			}
			if (p2CanCarry && !p2CanSire)
			{
				// pawn2 can only carry, so pawn1 sires
				carrier = pawn2;
				sirer = pawn1;
				return;
			}
		}
		if (p2CanCarry && p2CanSire)
		{
			// pawn2 can do both
			if (p1CanSire && !p1CanCarry)
			{
				// pawn1 can only sire, so pawn2 carries
				carrier = pawn2;
				sirer = pawn1;
				return;
			}
			if (p1CanCarry && !p1CanSire)
			{
				// pawn1 can only carry, so pawn2 sires
				carrier = pawn1;
				sirer = pawn2;
				return;
			}
		}

		// Case 3: Both can do both - assign different roles to different pawns
		// Use pawn1 as carrier, pawn2 as sirer (consistent arbitrary choice)
		if (p1CanCarry && p1CanSire && p2CanCarry && p2CanSire)
		{
			carrier = pawn1;
			sirer = pawn2;
			SimpleTransDebug.Log($"Both pawns have both capabilities - assigned {pawn1?.Name?.ToStringShort} as carrier, {pawn2?.Name?.ToStringShort} as sirer", 2);
			return;
		}

		// Case 4: Only one pawn can fill a role
		if (p1CanCarry && !p2CanCarry)
			carrier = pawn1;
		else if (p2CanCarry)
			carrier = pawn2;

		if (p1CanSire && !p2CanSire)
			sirer = pawn1;
		else if (p2CanSire)
			sirer = pawn2;

		// Case 5: Single pawn scenarios
		if (pawn2 == null)
		{
			if (p1CanCarry) carrier = pawn1;
			if (p1CanSire) sirer = pawn1;
		}
		else if (pawn1 == null)
		{
			if (p2CanCarry) carrier = pawn2;
			if (p2CanSire) sirer = pawn2;
		}
	}

	/// <summary>
	/// Gets base fertility excluding age factors (from genes, hediffs, etc.)
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>Base fertility value</returns>
	private static float GetBaseFertilityWithoutAge(Pawn pawn)
	{
		// Get the vanilla fertility stat
		float vanillaFertility = pawn.GetStatValue(StatDefOf.Fertility);

		// The vanilla stat includes age factor based on gender - we need to undo it
		// Get what vanilla thinks the age factor is (based on gender)
		float vanillaAgeFactor = pawn.gender == Gender.Female
			? CarryFertilityAgeCurve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat)
			: SireFertilityAgeCurve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);

		// Avoid division by zero
		if (vanillaAgeFactor <= 0.001f)
		{
			// If vanilla age factor is ~0, the pawn would be infertile by age
			// Return a reasonable base so we can apply our own age factor
			return 1.0f;
		}

		// Remove vanilla's age factor to get base fertility
		float baseFertility = vanillaFertility / vanillaAgeFactor;

		return baseFertility;
	}

	#endregion
}
