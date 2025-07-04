using System;
using System.Reflection;
using HarmonyLib;
using NonBinaryGender;
using VEF.Genes;
using Verse;

namespace Simple_Trans.Patches;

public static class NBGPatches
{
	public static void PatchNBG(this Harmony harmony)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected O, but got Unknown
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected O, but got Unknown
		harmony.Patch((MethodBase)typeof(SimpleTransPregnancyUtility).GetMethod("ValidateOrSetGenderWithGenes"), new HarmonyMethod((Delegate)new Action<Pawn, Gene>(ValidateOrSetGenderWithGenesPrefix)), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
		harmony.Patch((MethodBase)typeof(SimpleTransPregnancyUtility).GetMethod("SetCis"), new HarmonyMethod((Delegate)new Action<Pawn>(SetCisPrefix)), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
		harmony.Patch((MethodBase)typeof(SimpleTransPregnancyUtility).GetMethod("DecideTransgender"), (HarmonyMethod)null, new HarmonyMethod(DecideTransgenderPostfix), (HarmonyMethod)null, (HarmonyMethod)null);
	}

	public static void ValidateOrSetGenderWithGenesPrefix(Pawn pawn, Gene gene)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Invalid comparison between Unknown and I4
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Invalid comparison between Unknown and I4
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		GeneExtension modExtension = ((Def)gene.def).GetModExtension<GeneExtension>();
		if (modExtension == null)
		{
			return;
		}
		if (modExtension.forceFemale)
		{
			if (!EnbyUtility.IsEnby(pawn))
			{
				pawn.gender = (Gender)((Rand.Range(0f, 1f) > SimpleTransPregnancyUtility.cisRate) ? 1 : 2);
			}
			if ((int)pawn.gender == 1 || EnbyUtility.IsEnby(pawn))
			{
				SimpleTransPregnancyUtility.SetTrans(pawn);
			}
			else
			{
				SimpleTransPregnancyUtility.SetCis(pawn);
			}
			SimpleTransPregnancyUtility.SetCarry(pawn, removeSire: false);
		}
		if (modExtension.forceMale)
		{
			if (!EnbyUtility.IsEnby(pawn))
			{
				pawn.gender = (Gender)((!(Rand.Range(0f, 1f) > SimpleTransPregnancyUtility.cisRate)) ? 1 : 2);
			}
			if ((int)pawn.gender == 2 || EnbyUtility.IsEnby(pawn))
			{
				SimpleTransPregnancyUtility.SetTrans(pawn);
			}
			else
			{
				SimpleTransPregnancyUtility.SetCis(pawn);
			}
			SimpleTransPregnancyUtility.SetSire(pawn, removeCarry: false);
		}
	}

	public static void DecideTransgenderPostfix(ref bool __result, Pawn pawn)
	{
		__result = __result || EnbyUtility.IsEnby(pawn);
	}

	public static void SetCisPrefix(Pawn pawn)
	{
		if (EnbyUtility.IsEnby(pawn))
		{
			Log.Warning("Setting nonbinary pawn to cisgender.");
		}
	}
}
