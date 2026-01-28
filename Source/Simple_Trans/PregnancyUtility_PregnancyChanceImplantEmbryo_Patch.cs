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
	/// Includes bionic perfect control for implantation
	/// </summary>
	/// <param name="__result">The calculated implantation chance</param>
	/// <param name="surrogate">The pawn receiving the embryo</param>
	public static void Postfix(ref float __result, Pawn surrogate)
	{
		if (surrogate == null)
		{
			__result = 0f;
			return;
		}

		// Check if surrogate can actually carry pregnancies
		if (!SimpleTransHediffs.CanCarry(surrogate))
		{
			SimpleTransDebug.Log($"Embryo implantation failed: {surrogate?.Name?.ToStringShort ?? "null"} cannot carry", 2);
			__result = 0f;
			return;
		}

		// Bionic perfect control for embryo implantation
		if (SimpleTransHediffs.HasBionicCarry(surrogate))
		{
			// Bionic carriers have perfect control - guaranteed success
			// (Implantation is an explicit medical choice, so we assume they want it)
			SimpleTransDebug.Log($"Bionic implantation - guaranteed success for {surrogate.Name?.ToStringShort}", 2);
			__result = 1f;
			return;
		}

		// Normal fertility calculation using capability-based age curves
		float carryFertility = SimpleTransHediffs.GetCarryFertility(surrogate);

		// Apply implantation modifier (2x like vanilla)
		__result = UnityEngine.Mathf.Clamp01(carryFertility * 2f);

		SimpleTransDebug.Log($"Embryo implantation chance: {surrogate.Name?.ToStringShort} = {__result:F3} (fertility: {carryFertility:F3})", 2);
	}
}