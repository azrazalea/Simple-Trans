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
		// Determine actual carrier and sirer based on Simple Trans capabilities
		Pawn carrier = (SimpleTransPregnancyUtility.CanCarry(woman) ? woman : (SimpleTransPregnancyUtility.CanCarry(man) ? man : null));
		Pawn sirer = (SimpleTransPregnancyUtility.CanSire(man) ? man : (SimpleTransPregnancyUtility.CanSire(woman) ? woman : null));
		
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
			__result = 0f;
			return;
		}
		
		// BIONIC GUARANTEE: If both have bionic and both are trying for baby -> 100% chance
		if (carrierHasBionic && sirerHasBionic && 
		    carrierApproach == PregnancyApproach.TryForBaby && 
		    sirerApproach == PregnancyApproach.TryForBaby)
		{
			__result = 1f;
			return;
		}
		
		// Normal calculation with prosthetic modifiers
		float sirerChance = GetModifiedFertilityChance(sirer, true);
		float carrierChance = GetModifiedFertilityChance(carrier, false);
		float combinedChance = sirerChance * carrierChance;
		
		// Apply pregnancy approach factor
		float approachFactor = PregnancyUtility.GetPregnancyChanceFactor(carrierApproach);
		__result = approachFactor * combinedChance;
	}
	
	/// <summary>
	/// Gets fertility chance for a pawn with prosthetic-specific modifiers
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <param name="isSiring">True if checking siring ability, false if checking carrying ability</param>
	/// <returns>Modified fertility chance</returns>
	private static float GetModifiedFertilityChance(Pawn pawn, bool isSiring)
	{
		// Get base fertility chance
		float baseFertility = isSiring ? PregnancyUtility.PregnancyChanceForPawn(pawn) : PregnancyUtility.PregnancyChanceForWoman(pawn);
		
		// Apply prosthetic modifiers based on ability type
		float prostheticModifier = 0f;
		
		if (isSiring)
		{
			// Check for sire prosthetic modifiers
			if (pawn.health.hediffSet.HasHediff(HediffDef.Named("BasicProstheticSire")))
			{
				prostheticModifier = -0.3f; // 30% reduction
			}
			else if (pawn.health.hediffSet.HasHediff(HediffDef.Named("BionicProstheticSire")))
			{
				prostheticModifier = 0.2f; // 20% increase
			}
		}
		else
		{
			// Check for carry prosthetic modifiers
			if (pawn.health.hediffSet.HasHediff(HediffDef.Named("BasicProstheticCarry")))
			{
				prostheticModifier = -0.3f; // 30% reduction
			}
			else if (pawn.health.hediffSet.HasHediff(HediffDef.Named("BionicProstheticCarry")))
			{
				prostheticModifier = 0.2f; // 20% increase
			}
		}
		
		// Apply modifier to base fertility
		return baseFertility * (1f + prostheticModifier);
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
