using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Simple_Trans;

[HarmonyPatch(typeof(PawnGenerator), "GenerateInitialHediffs")]
public class PawnGenerator_GenerateInitialHediffs_Patch
{
	public static void Prefix(ref PawnGenerationRequest request, out bool __state)
	{
		__state = request.AllowPregnant;
		request.AllowPregnant = false;
	}

	public static void Postfix(ref Pawn pawn, ref PawnGenerationRequest request, bool __state)
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		request.AllowPregnant = __state;
		SimpleTransPregnancyUtility.ValidateOrSetGender(pawn);
		if (!ModsConfig.BiotechActive || pawn.Dead || !request.AllowPregnant || !SimpleTransPregnancyUtility.CanCarry(pawn))
		{
			return;
		}
		float num = pawn.kindDef.humanPregnancyChance * PregnancyUtility.PregnancyChanceForWoman(pawn);
		if (Find.Storyteller.difficulty.ChildrenAllowed && pawn.ageTracker.AgeBiologicalYears >= 16 && request.AllowPregnant && Rand.Chance(num))
		{
			Hediff_Pregnant val = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, pawn, (BodyPartRecord)null);
			FloatRange generatedPawnPregnancyProgressRange = PregnancyUtility.GeneratedPawnPregnancyProgressRange;
			((Hediff)val).Severity = generatedPawnPregnancyProgressRange.RandomInRange;
			Pawn val2 = null;
			DirectPawnRelation val3 = default(DirectPawnRelation);
			if (!Rand.Chance(0.2f) && GenCollection.TryRandomElementByWeight<DirectPawnRelation>(pawn.relations.DirectRelations.Where((DirectPawnRelation r) => PregnancyUtility.BeingFatherWeightPerRelation.ContainsKey(r.def)), (Func<DirectPawnRelation, float>)((DirectPawnRelation r) => PregnancyUtility.BeingFatherWeightPerRelation[r.def]), out val3))
			{
				val2 = val3.otherPawn;
			}
			bool flag = default(bool);
			GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(val2, pawn, out flag);
			if (flag)
			{
				((HediffWithParents)val).SetParents((Pawn)null, val2, inheritedGeneSet);
				pawn.health.AddHediff((Hediff)(object)val, (BodyPartRecord)null, (DamageInfo?)null);
			}
		}
	}
}
