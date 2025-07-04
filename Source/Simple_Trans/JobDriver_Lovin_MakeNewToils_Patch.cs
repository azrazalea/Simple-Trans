using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Simple_Trans;

[HarmonyPatch(typeof(JobDriver_Lovin), "MakeNewToils")]
public class JobDriver_Lovin_MakeNewToils_Patch
{
	public static void Postfix(ref IEnumerable<Toil> __result, JobDriver_Lovin __instance, float ___PregnancyChance, SimpleCurve ___LovinIntervalHoursFromAgeCurve, Job ___job, TargetIndex ___PartnerInd)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		Toil obj = __result.LastOrDefault();
		Pawn Partner = (Pawn)(Thing)___job.GetTarget(___PartnerInd);
		obj.AddFinishAction((Action)delegate
		{
			//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ed: Invalid comparison between Unknown and I4
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f6: Invalid comparison between Unknown and I4
			//IL_0137: Unknown result type (might be due to invalid IL or missing references)
			//IL_013e: Expected O, but got Unknown
			//IL_017d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0188: Unknown result type (might be due to invalid IL or missing references)
			//IL_018d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0197: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01be: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e0: Expected O, but got Unknown
			if (ModsConfig.BiotechActive)
			{
				Pawn val = (SimpleTransPregnancyUtility.CanSire(((JobDriver)__instance).pawn) ? ((JobDriver)__instance).pawn : (SimpleTransPregnancyUtility.CanSire(Partner) ? Partner : null));
				Pawn val2 = (SimpleTransPregnancyUtility.CanCarry(Partner) ? Partner : (SimpleTransPregnancyUtility.CanCarry(((JobDriver)__instance).pawn) ? ((JobDriver)__instance).pawn : null));
				string text = "Runaway.SimpleTrans.InPatch: Tried pregnancy calc for " + ((object)Partner.Name)?.ToString() + " and " + ((object)((JobDriver)__instance).pawn.Name)?.ToString() + ".";
				if (SimpleTrans.debugMode)
				{
					Log.Message(text);
				}
				if ((val == null || val2 == null || (int)val.gender != 1 || (int)val2.gender != 2) && val != null && val2 != null && Rand.Chance(___PregnancyChance * PregnancyUtility.PregnancyChanceForPartners(val2, val)))
				{
					bool flag = default(bool);
					GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(val, val2, out flag);
					if (flag)
					{
						Hediff_Pregnant val3 = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, val2, (BodyPartRecord)null);
						((HediffWithParents)val3).SetParents((Pawn)null, val, inheritedGeneSet);
						val2.health.AddHediff((Hediff)(object)val3, (BodyPartRecord)null, (DamageInfo?)null);
					}
					else if (PawnUtility.ShouldSendNotificationAbout(val) || PawnUtility.ShouldSendNotificationAbout(val2))
					{
						Messages.Message(TranslatorFormattedStringExtensions.Translate("MessagePregnancyFailed", NamedArgumentUtility.Named((object)val, "FATHER"), NamedArgumentUtility.Named((object)val2, "MOTHER")) + ": " + Translator.Translate("CombinedGenesExceedMetabolismLimits"), new LookTargets((TargetInfo[])(object)new TargetInfo[2]
						{
							(Thing)(object)val,
							(Thing)(object)val2
						}), MessageTypeDefOf.NegativeEvent, true);
					}
				}
			}
		});
	}
}
