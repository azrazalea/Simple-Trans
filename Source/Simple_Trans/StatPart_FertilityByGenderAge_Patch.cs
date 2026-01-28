using HarmonyLib;
using RimWorld;
using Verse;

namespace Simple_Trans;

/// <summary>
/// Patches StatPart_FertilityByGenderAge to use reproductive capability instead of gender
/// for determining which age curve to apply to fertility calculations.
/// This fixes the stat display so players see correct fertility values.
/// </summary>
[HarmonyPatch(typeof(StatPart_FertilityByGenderAge))]
public static class StatPart_FertilityByGenderAge_Patch
{
	/// <summary>
	/// Prefix to override TransformValue with capability-based age factor
	/// </summary>
	[HarmonyPatch("TransformValue")]
	[HarmonyPrefix]
	public static bool TransformValue_Prefix(StatRequest req, ref float val)
	{
		if (req.Thing is Pawn pawn && pawn != null)
		{
			float ageFactor = GetCapabilityBasedAgeFactor(pawn);
			val *= ageFactor;
			return false; // Skip original method
		}
		return true; // Let original handle non-pawn cases
	}

	/// <summary>
	/// Prefix to override ExplanationPart with capability-based age factor
	/// </summary>
	[HarmonyPatch("ExplanationPart")]
	[HarmonyPrefix]
	public static bool ExplanationPart_Prefix(StatRequest req, ref string __result)
	{
		if (req.Thing is Pawn pawn && pawn != null)
		{
			float ageFactor = GetCapabilityBasedAgeFactor(pawn);
			__result = "StatsReport_FertilityAgeFactor".Translate() + ": x" + ageFactor.ToStringPercent();
			return false; // Skip original method
		}
		return true;
	}

	/// <summary>
	/// Gets the age factor based on reproductive capability instead of gender.
	/// Uses the shared helper methods from SimpleTransHediffs.
	/// </summary>
	private static float GetCapabilityBasedAgeFactor(Pawn pawn)
	{
		bool canCarry = SimpleTransHediffs.CanCarry(pawn);
		bool canSire = SimpleTransHediffs.CanSire(pawn);

		// Capability-based age factor selection
		if (canCarry && canSire)
		{
			// Has both - show the more restrictive (carry) curve for display
			return SimpleTransHediffs.GetCarryAgeFactor(pawn);
		}
		else if (canCarry)
		{
			return SimpleTransHediffs.GetCarryAgeFactor(pawn);
		}
		else if (canSire)
		{
			return SimpleTransHediffs.GetSireAgeFactor(pawn);
		}

		// No reproductive capability - use carry age factor as fallback
		return SimpleTransHediffs.GetCarryAgeFactor(pawn);
	}
}
