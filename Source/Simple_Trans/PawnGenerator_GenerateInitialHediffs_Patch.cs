using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Simple_Trans;

/// <summary>
/// Harmony patch for PawnGenerator.GenerateInitialHediffs
/// Handles pregnancy generation for pawns with appropriate reproductive capabilities
/// </summary>
[HarmonyPatch(typeof(PawnGenerator), "GenerateInitialHediffs")]
public class PawnGenerator_GenerateInitialHediffs_Patch
{
	/// <summary>
	/// Prefix method to temporarily disable pregnancy generation
	/// We handle pregnancy generation ourselves in the postfix
	/// </summary>
	/// <param name="request">The pawn generation request</param>
	/// <param name="__state">Stores the original AllowPregnant state</param>
	public static void Prefix(ref PawnGenerationRequest request, out bool __state)
	{
		try
		{
			__state = request.AllowPregnant;
			request.AllowPregnant = false;
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error in GenerateInitialHediffs Prefix: {ex}");
			__state = false;
		}
	}

	/// <summary>
	/// Postfix method to handle pregnancy generation with Simple Trans logic
	/// </summary>
	/// <param name="pawn">The generated pawn</param>
	/// <param name="request">The pawn generation request</param>
	/// <param name="__state">The original AllowPregnant state</param>
	public static void Postfix(ref Pawn pawn, ref PawnGenerationRequest request, bool __state)
	{
		try
		{
			request.AllowPregnant = __state;

			if (pawn == null)
			{
				Log.Error("[Simple Trans] GenerateInitialHediffs Postfix called with null pawn");
				return;
			}

			// Early exit if pregnancy is not applicable
			if (!ModsConfig.BiotechActive || pawn.Dead || !request.AllowPregnant || !SimpleTransHediffs.CanCarry(pawn))
			{
				return;
			}

			// Validate required objects exist
			if (pawn.kindDef == null || pawn.ageTracker == null || Find.Storyteller?.difficulty == null)
			{
				SimpleTransDebug.Log("Missing required objects for pregnancy generation", 2);
				return;
			}

			// Calculate pregnancy chance
			float pregnancyChance = pawn.kindDef.humanPregnancyChance * PregnancyUtility.PregnancyChanceForWoman(pawn);

			// Check if pregnancy should be generated
			if (Find.Storyteller.difficulty.ChildrenAllowed &&
				pawn.ageTracker.AgeBiologicalYears >= SimpleTransConstants.MinimumReproductiveAge &&
				request.AllowPregnant &&
				Rand.Chance(pregnancyChance))
			{
				SimpleTransPregnancy.TryCreatePregnancy(pawn, findRandomFather: true);
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error in GenerateInitialHediffs Postfix for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}

}
