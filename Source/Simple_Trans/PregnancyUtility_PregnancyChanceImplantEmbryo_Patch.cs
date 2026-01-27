using HarmonyLib;
using RimWorld;
using Verse;

namespace Simple_Trans;

/// <summary>
/// Harmony patch for PregnancyUtility.PregnancyChanceImplantEmbryo
/// Provides complete control over embryo implantation success rates using Simple Trans capabilities
/// </summary>
[HarmonyPatch(typeof(PregnancyUtility), "PregnancyChanceImplantEmbryo")]
public class PregnancyUtility_PregnancyChanceImplantEmbryo_Patch
{
	/// <summary>
	/// Postfix method to recalculate embryo implantation chance based on Simple Trans carry capability
	/// </summary>
	/// <param name="__result">The calculated implantation chance</param>
	/// <param name="surrogate">The pawn receiving the embryo</param>
	public static void Postfix(ref float __result, Pawn surrogate)
	{
		// Check if surrogate can actually carry pregnancies
		if (!SimpleTransHediffs.CanCarry(surrogate))
		{
			SimpleTransDebug.Log($"Embryo implantation failed: {surrogate?.Name?.ToStringShort ?? "null"} cannot carry", 2);
			__result = 0f;
			return;
		}
		
		// Calculate base fertility for carrying capability
		float baseFertility = GetCarryCapabilityFertility(surrogate);
		
		// Apply implantation modifier (2x like vanilla, but based on our fertility calculation)
		float implantationChance = UnityEngine.Mathf.Clamp01(baseFertility * 2f);
		
		// Check for bionic carry prosthetic - provides enhanced implantation success
		if (HasBionicCarryProsthetic(surrogate))
		{
			// Bionic prosthetic guarantees higher success rate
			implantationChance = UnityEngine.Mathf.Clamp01(implantationChance * 1.5f);
			SimpleTransDebug.Log($"Bionic carry prosthetic bonus applied: {implantationChance:F3}", 2);
		}
		
		__result = implantationChance;
		
		SimpleTransDebug.Log($"Embryo implantation chance: {surrogate?.Name?.ToStringShort ?? "null"} = {__result:F3} (base: {baseFertility:F3})", 2);
	}
	
	/// <summary>
	/// Gets fertility specifically for carrying capability, bypassing vanilla PregnancyChanceForPawn
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>Fertility chance for carrying pregnancies</returns>
	private static float GetCarryCapabilityFertility(Pawn pawn)
	{
		// Start with base fertility stat
		float baseFertility = pawn.GetStatValue(StatDefOf.Fertility);
		
		// Apply prosthetic modifiers specific to carrying
		if (pawn.health.hediffSet.HasHediff(HediffDef.Named("BasicProstheticCarry")))
		{
			baseFertility *= 0.7f; // 30% reduction for basic prosthetic
		}
		else if (pawn.health.hediffSet.HasHediff(HediffDef.Named("BionicProstheticCarry")))
		{
			baseFertility *= 1.2f; // 20% increase for bionic prosthetic
		}
		
		// Check for capability-specific sterilization
		var sterilizedCarryDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedCarry");
		if (sterilizedCarryDef != null && pawn.health.hediffSet.HasHediff(sterilizedCarryDef))
		{
			return 0f; // Sterilized for carrying
		}
		
		// Check for general sterilization
		if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Sterilized) != null)
		{
			return 0f; // Generally sterilized
		}
		
		return baseFertility;
	}
	
	/// <summary>
	/// Checks if a pawn has bionic carry prosthetics
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>True if pawn has bionic carry prosthetics</returns>
	private static bool HasBionicCarryProsthetic(Pawn pawn)
	{
		return pawn.health.hediffSet.HasHediff(HediffDef.Named("BionicProstheticCarry"));
	}
}