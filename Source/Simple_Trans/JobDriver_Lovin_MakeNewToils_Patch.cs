using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Simple_Trans;

/// <summary>
/// Harmony patch for JobDriver_Lovin.MakeNewToils
/// 
/// Purpose: Extends RimWorld's lovin/romance system to support Simple Trans reproductive logic
/// 
/// Background: Vanilla RimWorld only creates pregnancies from Male+Female gender combinations.
/// Simple Trans introduces transgender pawns whose gender identity may differ from their
/// reproductive capabilities. This patch enables pregnancy creation for non-standard
/// combinations that vanilla would ignore.
/// 
/// Supported Combinations:
/// - Trans male (Male gender, can carry) + anyone who can sire
/// - Trans female (Female gender, can sire) + anyone who can carry  
/// - Any combination where reproductive capabilities don't match vanilla gender expectations
/// 
/// Implementation: Hooks into the final toil of the lovin job to check for Simple Trans
/// reproductive capabilities and create pregnancies for compatible non-standard pairings.
/// Standard cis male+female combinations are left to vanilla logic unchanged.
/// 
/// Technical Details: Uses postfix to modify the toil list, adding a finish action that
/// evaluates reproductive compatibility and handles pregnancy creation with proper gene
/// inheritance and compatibility checking.
/// </summary>
[HarmonyPatch(typeof(JobDriver_Lovin), "MakeNewToils")]
public class JobDriver_Lovin_MakeNewToils_Patch
{
	/// <summary>
	/// Postfix method to add pregnancy handling logic to the lovin job
	/// </summary>
	/// <param name="__result">The enumerable of toils from the original method</param>
	/// <param name="__instance">The JobDriver_Lovin instance</param>
	/// <param name="___PregnancyChance">The pregnancy chance field</param>
	/// <param name="___LovinIntervalHoursFromAgeCurve">The lovin interval curve field</param>
	/// <param name="___job">The job field</param>
	/// <param name="___PartnerInd">The partner target index field</param>
	public static void Postfix(ref IEnumerable<Toil> __result, JobDriver_Lovin __instance, float ___PregnancyChance, SimpleCurve ___LovinIntervalHoursFromAgeCurve, Job ___job, TargetIndex ___PartnerInd)
	{
		try
		{
			if (__result == null || __instance?.pawn == null || ___job == null)
			{
				Log.Error("[Simple Trans] JobDriver_Lovin patch called with null parameters");
				return;
			}
			
			Toil finalToil = __result.LastOrDefault();
			if (finalToil == null)
			{
				SimpleTransDebug.Log("No final toil found in JobDriver_Lovin", 2);
				return;
			}
			
			Pawn partner = (Pawn)(Thing)___job.GetTarget(___PartnerInd);
			
			finalToil.AddFinishAction((Action)delegate
			{
				HandlePregnancyChance(__instance.pawn, partner, ___PregnancyChance);
			});
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error in JobDriver_Lovin patch: {ex}");
		}
	}
	
	/// <summary>
	/// Handles pregnancy chance calculation and creation for Simple Trans reproductive capabilities
	/// </summary>
	/// <param name="pawn1">First pawn in the relationship</param>
	/// <param name="pawn2">Second pawn in the relationship</param>
	/// <param name="basePregnancyChance">The base pregnancy chance</param>
	private static void HandlePregnancyChance(Pawn pawn1, Pawn pawn2, float basePregnancyChance)
	{
		try
		{
			// Validate input parameters
			if (pawn1 == null || pawn2 == null)
			{
				Log.Error("[Simple Trans] HandlePregnancyChance called with null pawns");
				return;
			}
			
			// Only proceed if Biotech is active
			if (!ModsConfig.BiotechActive)
			{
				return;
			}
			
			// Determine reproductive roles
			Pawn sirer = (SimpleTransPregnancyUtility.CanSire(pawn1) ? pawn1 : (SimpleTransPregnancyUtility.CanSire(pawn2) ? pawn2 : null));
			Pawn carrier = (SimpleTransPregnancyUtility.CanCarry(pawn2) ? pawn2 : (SimpleTransPregnancyUtility.CanCarry(pawn1) ? pawn1 : null));
			
			// Debug logging
			SimpleTransDebug.Log($"Trying pregnancy calculation for {pawn2?.Name?.ToStringShort ?? "unknown"} and {pawn1?.Name?.ToStringShort ?? "unknown"}", 2);
			
			// Check if pregnancy is possible and should occur
			if (ShouldCreatePregnancy(sirer, carrier, basePregnancyChance))
			{
				TryCreatePregnancy(sirer, carrier);
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error handling pregnancy chance for {pawn1?.Name?.ToStringShort ?? "unknown"} and {pawn2?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}
	
	/// <summary>
	/// Determines if pregnancy should be created based on reproductive capabilities and chance
	/// </summary>
	/// <param name="sirer">The pawn that can sire</param>
	/// <param name="carrier">The pawn that can carry</param>
	/// <param name="basePregnancyChance">The base pregnancy chance</param>
	/// <returns>True if pregnancy should be created</returns>
	private static bool ShouldCreatePregnancy(Pawn sirer, Pawn carrier, float basePregnancyChance)
	{
		try
		{
			// Both sirer and carrier must exist
			if (sirer == null || carrier == null)
			{
				return false;
			}
			
			// Determine if this reproductive combination differs from vanilla expectations
			// Vanilla RimWorld only supports Male+Female combinations for pregnancy
			// Simple Trans enables additional combinations based on reproductive capabilities:
			// - Trans male (Male gender) + anyone with siring ability
			// - Trans female (Female gender) + anyone with carrying ability
			// - Other non-standard gender/reproductive combinations
			bool isNonStandardCombination = (sirer.gender != Gender.Male || carrier.gender != Gender.Female);
			
			// Let vanilla RimWorld handle standard cis male + cis female combinations
			// Only intercept and process non-standard combinations that vanilla would reject
			if (!isNonStandardCombination)
			{
				return false; // Vanilla logic handles cis male + cis female perfectly fine
			}
			
			// Calculate pregnancy chance
			float pregnancyChance = basePregnancyChance * PregnancyUtility.PregnancyChanceForPartners(carrier, sirer);
			return Rand.Chance(pregnancyChance);
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error determining pregnancy eligibility for {sirer?.Name?.ToStringShort ?? "unknown"} and {carrier?.Name?.ToStringShort ?? "unknown"}: {ex}");
			return false;
		}
	}
	
	/// <summary>
	/// Attempts to create a pregnancy with gene compatibility checking
	/// </summary>
	/// <param name="sirer">The pawn that sires the pregnancy</param>
	/// <param name="carrier">The pawn that carries the pregnancy</param>
	private static void TryCreatePregnancy(Pawn sirer, Pawn carrier)
	{
		try
		{
			if (sirer == null || carrier == null)
			{
				Log.Error("[Simple Trans] TryCreatePregnancy called with null pawns");
				return;
			}
			
			if (carrier.health == null)
			{
				Log.Error($"[Simple Trans] Carrier {carrier.Name?.ToStringShort ?? "unknown"} has no health component");
				return;
			}
			
			bool genesCompatible = default(bool);
			GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(sirer, carrier, out genesCompatible);
			
			if (genesCompatible)
			{
				// Create and apply pregnancy
				Hediff_Pregnant pregnancy = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, carrier, (BodyPartRecord)null);
				if (pregnancy == null)
				{
					Log.Error("[Simple Trans] Failed to create pregnancy hediff");
					return;
				}
				
				((HediffWithParents)pregnancy).SetParents((Pawn)null, sirer, inheritedGeneSet);
				carrier.health.AddHediff((Hediff)(object)pregnancy, (BodyPartRecord)null, (DamageInfo?)null);
				
				SimpleTransDebug.Log($"Successfully created pregnancy: {carrier?.Name?.ToStringShort ?? "unknown"} carrying {sirer?.Name?.ToStringShort ?? "unknown"}'s child", 2);
			}
			else if (PawnUtility.ShouldSendNotificationAbout(sirer) || PawnUtility.ShouldSendNotificationAbout(carrier))
			{
				// Send failure notification
				TaggedString message = TranslatorFormattedStringExtensions.Translate("MessagePregnancyFailed", 
					NamedArgumentUtility.Named((object)sirer, "FATHER"), 
					NamedArgumentUtility.Named((object)carrier, "MOTHER")) + ": " + Translator.Translate("CombinedGenesExceedMetabolismLimits");
				
				Messages.Message(message, new LookTargets((TargetInfo[])(object)new TargetInfo[2]
				{
					(Thing)(object)sirer,
					(Thing)(object)carrier
				}), MessageTypeDefOf.NegativeEvent, true);
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error creating pregnancy for {sirer?.Name?.ToStringShort ?? "unknown"} and {carrier?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}
}
