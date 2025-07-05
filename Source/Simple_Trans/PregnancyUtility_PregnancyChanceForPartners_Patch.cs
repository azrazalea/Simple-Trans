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
		
		// Calculate individual chances
		float sirerChance = ((sirer == null) ? 1f : PregnancyUtility.PregnancyChanceForPawn(sirer));
		float carrierChance = ((carrier == null || carrier == sirer) ? 0f : PregnancyUtility.PregnancyChanceForWoman(carrier));
		float combinedChance = sirerChance * carrierChance;
		
		// Apply pregnancy approach factor
		float approachFactor = ((carrier != null) ? PregnancyUtility.GetPregnancyChanceFactor(carrier.relations.GetPregnancyApproachForPartner(sirer)) : 0f);
		__result = approachFactor * combinedChance;
	}
}
