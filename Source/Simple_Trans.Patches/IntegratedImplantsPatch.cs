using HarmonyLib;
using RimWorld;
using Verse;

namespace Simple_Trans.Patches;

/// <summary>
/// Harmony postfix patch for Recipe_InstallImplant.ApplyOnPawn
///
/// Purpose: Removes Simple Trans carry hediffs when Integrated Implants wombs are installed
///
/// Background: When a pawn receives a Synthwomb or Archowomb from Integrated Implants,
/// it replaces any existing reproductive capability. This patch ensures that Simple Trans
/// carry hediffs (PregnancyCarry, BasicProstheticCarry, BionicProstheticCarry) are removed
/// to prevent duplicate capability sources.
/// </summary>
[HarmonyPatch(typeof(Recipe_InstallImplant), nameof(Recipe_InstallImplant.ApplyOnPawn))]
public static class IntegratedImplants_InstallWomb_Patch
{
	/// <summary>
	/// Postfix patch that removes Simple Trans carry hediffs when a womb is installed
	/// </summary>
	/// <param name="pawn">The pawn receiving the implant</param>
	/// <param name="___recipe">The recipe being applied</param>
	[HarmonyPostfix]
	public static void Postfix(Pawn pawn, RecipeDef ___recipe)
	{
		// Only run if Integrated Implants is active
		if (!SimpleTrans.IntegratedImplantsActive)
			return;

		// Only act on Synthwomb/Archowomb installation
		if (___recipe?.addsHediff?.defName != "Synthwomb" &&
			___recipe?.addsHediff?.defName != "Archowomb")
			return;

		SimpleTransDebug.Log($"Integrated Implants womb installed on {pawn.Name?.ToStringShort}, removing Simple Trans carry hediffs", 2);

		// Remove Simple Trans carry hediffs
		RemoveHediffIfPresent(pawn, "PregnancyCarry");
		RemoveHediffIfPresent(pawn, "BasicProstheticCarry");
		RemoveHediffIfPresent(pawn, "BionicProstheticCarry");
	}

	/// <summary>
	/// Removes a hediff from a pawn if present
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <param name="defName">The hediff def name to remove</param>
	private static void RemoveHediffIfPresent(Pawn pawn, string defName)
	{
		var def = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
		if (def == null)
			return;

		var hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(def);
		if (hediff != null)
		{
			pawn.health.RemoveHediff(hediff);
			SimpleTransDebug.Log($"Removed {defName} from {pawn.Name?.ToStringShort}", 3);
		}
	}
}
