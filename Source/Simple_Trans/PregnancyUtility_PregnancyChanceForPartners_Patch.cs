using HarmonyLib;
using RimWorld;
using Verse;

namespace Simple_Trans;

/// <summary>
/// Harmony patch for PregnancyUtility.PregnancyChanceForPartners
/// Modifies pregnancy chance calculation to work with Simple Trans reproductive capabilities
/// </summary>
[HarmonyPatch(typeof(PregnancyUtility), "PregnancyChanceForPartners")]
public class PregnancyUtility_PregnancyChanceForPartners_Patch
{
	/// <summary>
	/// Postfix method to recalculate pregnancy chance based on Simple Trans logic
	/// </summary>
	/// <param name="__result">The calculated pregnancy chance</param>
	/// <param name="woman">The female pawn parameter (may not actually be the carrier)</param>
	/// <param name="man">The male pawn parameter (may not actually be the sirer)</param>
	public static void Postfix(ref float __result, Pawn woman, Pawn man)
	{
		// Use affirming local variable names instead of assuming gender roles
		Pawn first = woman;
		Pawn second = man;

		// Determine actual carrier and sirer based on Simple Trans capabilities
		bool firstCanCarry = SimpleTransPregnancyUtility.CanCarry(first);
		bool firstCanSire = SimpleTransPregnancyUtility.CanSire(first);
		bool secondCanCarry = SimpleTransPregnancyUtility.CanCarry(second);
		bool secondCanSire = SimpleTransPregnancyUtility.CanSire(second);

		Pawn carrier = (firstCanCarry ? first : (secondCanCarry ? second : null));
		Pawn sirer = (secondCanSire ? second : (firstCanSire ? first : null));

		SimpleTransDebug.Log($"Pregnancy chance calculation: {first?.Name?.ToStringShort} + {second?.Name?.ToStringShort} -> Carrier: {carrier?.Name?.ToStringShort ?? "none"}, Sirer: {sirer?.Name?.ToStringShort ?? "none"} (base: {__result:F3})", 2);

		if (carrier == null || sirer == null)
		{
			__result = 0f;
			return;
		}

		// Check pregnancy approach
		PregnancyApproach carrierApproach = carrier.relations.GetPregnancyApproachForPartner(sirer);
		PregnancyApproach sirerApproach = sirer.relations.GetPregnancyApproachForPartner(carrier);

		// Check for bionic prosthetics
		bool carrierHasBionic = HasBionicReproductiveProsthetic(carrier, false);
		bool sirerHasBionic = HasBionicReproductiveProsthetic(sirer, true);

		// BIONIC PREVENTION: If either has bionic and is avoiding pregnancy -> 0% chance
		if ((carrierHasBionic && carrierApproach == PregnancyApproach.AvoidPregnancy) ||
			(sirerHasBionic && sirerApproach == PregnancyApproach.AvoidPregnancy))
		{
			SimpleTransDebug.Log($"Bionic contraception active - pregnancy prevented", 2);
			__result = 0f;
			return;
		}

		// BIONIC GUARANTEE: If both have bionic and both are trying for baby -> 100% chance
		if (carrierHasBionic && sirerHasBionic &&
			carrierApproach == PregnancyApproach.TryForBaby &&
			sirerApproach == PregnancyApproach.TryForBaby)
		{
			SimpleTransDebug.Log($"Bionic fertility enhancement active - pregnancy guaranteed", 2);
			__result = 20f;
			return;
		}

		// Check if the base system returned zero (likely due to gender/capability confusion)
		// Note: 1.0 is fine (means no fertility issues), only 0.0 indicates system confusion
		float baseChance;
		if (__result <= 0.001f)
		{
			// Base system thinks pregnancy is impossible - calculate our own realistic base chance
			baseChance = CalculateRealisticBaseChance(carrier, sirer);
			SimpleTransDebug.Log($"Base system confusion - recalculated chance: {baseChance:F3}", 2);
		}
		else
		{
			// Use the existing result (from VEF or vanilla) as our base chance
			// This includes 1.0 (no fertility issues) and normal values like 0.805
			baseChance = __result;
		}

		// Apply our capability-based modifiers on top of the base chance
		float sirerModifier = GetCapabilityModifier(sirer, true);
		float carrierModifier = GetCapabilityModifier(carrier, false);
		float combinedModifier = sirerModifier * carrierModifier;

		float originalResult = __result;
		__result = baseChance * combinedModifier;

		if (combinedModifier != 1.0f)
		{
			SimpleTransDebug.Log($"Capability modifiers applied: {baseChance:F3} * {combinedModifier:F3} = {__result:F3}", 2);
		}
	}

	/// <summary>
	/// Gets capability-based modifier for a pawn's reproductive ability
	/// Returns a multiplier to apply to existing pregnancy chances
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <param name="isSiring">True if checking siring ability, false if checking carrying ability</param>
	/// <returns>Capability modifier (1.0 = no change, 0.0 = sterile, >1.0 = enhanced)</returns>
	private static float GetCapabilityModifier(Pawn pawn, bool isSiring)
	{
		// Check if pawn has the required capability
		if (isSiring && !SimpleTransPregnancyUtility.CanSire(pawn))
		{
			return 0f; // Cannot sire at all
		}
		if (!isSiring && !SimpleTransPregnancyUtility.CanCarry(pawn))
		{
			return 0f; // Cannot carry at all
		}

		// Start with base capability modifier (1.0 = normal)
		float modifier = 1f;

		// Apply prosthetic modifiers based on ability type
		if (isSiring)
		{
			// Check for sire prosthetic modifiers
			if (pawn.health.hediffSet.HasHediff(HediffDef.Named("BasicProstheticSire")))
			{
				modifier *= 0.7f; // 30% reduction
			}
			else if (pawn.health.hediffSet.HasHediff(HediffDef.Named("BionicProstheticSire")))
			{
				modifier *= 1.2f; // 20% increase
			}
		}
		else
		{
			// Check for carry prosthetic modifiers
			if (pawn.health.hediffSet.HasHediff(HediffDef.Named("BasicProstheticCarry")))
			{
				modifier *= 0.7f; // 30% reduction
			}
			else if (pawn.health.hediffSet.HasHediff(HediffDef.Named("BionicProstheticCarry")))
			{
				modifier *= 1.2f; // 20% increase
			}
		}

		return modifier;
	}

	/// <summary>
	/// Calculates a realistic base pregnancy chance when the base system returns extreme values
	/// Uses vanilla pregnancy calculation but with capability-based role assignment
	/// </summary>
	/// <param name="carrier">The pawn with carry capability</param>
	/// <param name="sirer">The pawn with sire capability</param>
	/// <returns>Realistic base pregnancy chance</returns>
	private static float CalculateRealisticBaseChance(Pawn carrier, Pawn sirer)
	{
		try
		{
			// Use vanilla's internal pregnancy calculation methods with correct role assignment
			// Get carrier's fertility (using PregnancyChanceForWoman regardless of actual gender)
			float carrierFertility = PregnancyUtility.PregnancyChanceForWoman(carrier);

			// Get sirer's fertility (using PregnancyChanceForPawn regardless of actual gender)  
			float sirerFertility = PregnancyUtility.PregnancyChanceForPawn(sirer);

			// Combine using vanilla's approach
			float combinedChance = carrierFertility * sirerFertility;

			// Apply pregnancy approach factor (use carrier's approach as they're the one getting pregnant)
			PregnancyApproach carrierApproach = carrier.relations.GetPregnancyApproachForPartner(sirer);
			float approachFactor = PregnancyUtility.GetPregnancyChanceFactor(carrierApproach);

			float result = combinedChance * approachFactor;

			SimpleTransDebug.Log($"Realistic pregnancy chance calculation: {result:F3} (fertility: {combinedChance:F3}, approach: {approachFactor:F3})", 3);

			return result;
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error calculating realistic base chance: {ex}");
			return 0.05f; // Fallback to reasonable default
		}
	}

	/// <summary>
	/// Checks if a pawn has bionic reproductive prosthetics
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <param name="isSiring">True if checking siring ability, false if checking carrying ability</param>
	/// <returns>True if pawn has bionic prosthetics for the specified ability</returns>
	private static bool HasBionicReproductiveProsthetic(Pawn pawn, bool isSiring)
	{
		if (isSiring)
		{
			return pawn.health.hediffSet.HasHediff(HediffDef.Named("BionicProstheticSire"));
		}
		else
		{
			return pawn.health.hediffSet.HasHediff(HediffDef.Named("BionicProstheticCarry"));
		}
	}
}
