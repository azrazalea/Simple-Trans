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
		// Rename to neutral terms immediately
		Pawn pawn1 = woman;
		Pawn pawn2 = man;

		// Null checks
		if (pawn1 == null || pawn2 == null)
		{
			__result = 0f;
			return;
		}

		// Determine roles using unified logic
		SimpleTransHediffs.DetermineRoles(pawn1, pawn2, out Pawn carrier, out Pawn sirer);

		SimpleTransDebug.Log($"Pregnancy chance calculation: {pawn1?.Name?.ToStringShort} + {pawn2?.Name?.ToStringShort} -> Carrier: {carrier?.Name?.ToStringShort ?? "none"}, Sirer: {sirer?.Name?.ToStringShort ?? "none"} (base: {__result:F3})", 2);

		if (carrier == null || sirer == null)
		{
			__result = 0f;
			return;
		}

		// Check pregnancy approach
		PregnancyApproach carrierApproach = carrier.relations.GetPregnancyApproachForPartner(sirer);
		PregnancyApproach sirerApproach = sirer.relations.GetPregnancyApproachForPartner(carrier);

		// Check for bionic prosthetics (for their relevant capability)
		bool carrierHasBionic = SimpleTransHediffs.HasBionicCarry(carrier);
		bool sirerHasBionic = SimpleTransHediffs.HasBionicSire(sirer);

		// BIONIC PERFECT CONTROL: If either has bionic, their preference is absolute
		// Avoid always wins (check first)
		if ((carrierHasBionic && carrierApproach == PregnancyApproach.AvoidPregnancy) ||
			(sirerHasBionic && sirerApproach == PregnancyApproach.AvoidPregnancy))
		{
			SimpleTransDebug.Log($"Bionic contraception active - pregnancy prevented", 2);
			__result = 0f;
			return;
		}

		// Try for baby guarantees pregnancy (unless carrier is already pregnant)
		if ((carrierHasBionic && carrierApproach == PregnancyApproach.TryForBaby) ||
			(sirerHasBionic && sirerApproach == PregnancyApproach.TryForBaby))
		{
			// Check if carrier is already pregnant
			if (carrier.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman))
			{
				SimpleTransDebug.Log($"Bionic trying for baby but carrier already pregnant", 2);
				__result = 0f;
				return;
			}

			SimpleTransDebug.Log($"Bionic fertility enhancement active - pregnancy guaranteed", 2);
			__result = 20f;
			return;
		}

		// Normal calculation using capability-based fertility with correct age curves
		__result = CalculateRealisticBaseChance(carrier, sirer);
	}

	/// <summary>
	/// Calculates pregnancy chance using capability-based fertility with correct age curves
	/// </summary>
	private static float CalculateRealisticBaseChance(Pawn carrier, Pawn sirer)
	{
		try
		{
			float carrierFertility = SimpleTransHediffs.GetCarryFertility(carrier);
			float sirerFertility = SimpleTransHediffs.GetSireFertility(sirer);
			float combinedChance = carrierFertility * sirerFertility;

			// Apply pregnancy approach factors
			PregnancyApproach carrierApproach = carrier.relations.GetPregnancyApproachForPartner(sirer);
			PregnancyApproach sirerApproach = sirer.relations.GetPregnancyApproachForPartner(carrier);
			float carrierApproachFactor = PregnancyUtility.GetPregnancyChanceFactor(carrierApproach);
			float sirerApproachFactor = PregnancyUtility.GetPregnancyChanceFactor(sirerApproach);

			float result = combinedChance * carrierApproachFactor * sirerApproachFactor;

			SimpleTransDebug.Log($"Pregnancy chance: {result:F3} (carrier: {carrierFertility:F3}, sirer: {sirerFertility:F3}, approaches: {carrierApproachFactor:F3} * {sirerApproachFactor:F3})", 3);

			return result;
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error calculating pregnancy chance: {ex}");
			return 0.05f;
		}
	}
}
