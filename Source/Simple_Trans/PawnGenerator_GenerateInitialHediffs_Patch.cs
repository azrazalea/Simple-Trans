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
			if (!ModsConfig.BiotechActive || pawn.Dead || !request.AllowPregnant || !SimpleTransPregnancyUtility.CanCarry(pawn))
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
				TryCreatePregnancy(pawn);
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error in GenerateInitialHediffs Postfix for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}

	/// <summary>
	/// Safely attempts to create a pregnancy for the given pawn
	/// </summary>
	/// <param name="pawn">The pawn to make pregnant</param>
	private static void TryCreatePregnancy(Pawn pawn)
	{
		try
		{
			// Create pregnancy hediff
			Hediff_Pregnant pregnancy = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, pawn, (BodyPartRecord)null);
			if (pregnancy == null)
			{
				Log.Error("[Simple Trans] Failed to create pregnancy hediff");
				return;
			}

			FloatRange generatedPawnPregnancyProgressRange = PregnancyUtility.GeneratedPawnPregnancyProgressRange;
			((Hediff)pregnancy).Severity = generatedPawnPregnancyProgressRange.RandomInRange;

			// Determine father (if any)
			Pawn father = null;
			DirectPawnRelation fatherRelation = default(DirectPawnRelation);

			if (pawn.relations?.DirectRelations != null &&
				!Rand.Chance(SimpleTransConstants.RandomFatherlessChance) &&
				GenCollection.TryRandomElementByWeight<DirectPawnRelation>(
					pawn.relations.DirectRelations.Where((DirectPawnRelation r) => PregnancyUtility.BeingFatherWeightPerRelation.ContainsKey(r.def)),
					(Func<DirectPawnRelation, float>)((DirectPawnRelation r) => PregnancyUtility.BeingFatherWeightPerRelation[r.def]),
					out fatherRelation))
			{
				father = fatherRelation.otherPawn;
			}

			// Generate gene set and apply pregnancy if compatible
			bool genesCompatible = default(bool);
			GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(father, pawn, out genesCompatible);
			if (genesCompatible)
			{
				((HediffWithParents)pregnancy).SetParents((Pawn)null, father, inheritedGeneSet);
				pawn.health.AddHediff((Hediff)(object)pregnancy, (BodyPartRecord)null, (DamageInfo?)null);
				SimpleTransDebug.Log($"Created pregnancy for {pawn.Name?.ToStringShort ?? "unknown"} with father {father?.Name?.ToStringShort ?? "none"}", 3);
			}
			else
			{
				SimpleTransDebug.Log($"Genes incompatible for pregnancy between {pawn.Name?.ToStringShort ?? "unknown"} and {father?.Name?.ToStringShort ?? "none"}", 2);
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error creating pregnancy for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}
}
