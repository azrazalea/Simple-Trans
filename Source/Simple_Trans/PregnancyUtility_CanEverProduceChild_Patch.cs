using HarmonyLib;
using RimWorld;
using Verse;

namespace Simple_Trans;

[HarmonyPatch(typeof(PregnancyUtility), "CanEverProduceChild")]
public class PregnancyUtility_CanEverProduceChild_Patch
{
	public static void Postfix(ref AcceptanceReport __result, Pawn first, Pawn second)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		string reason = __result.Reason;
		TaggedString val = Translator.Translate("SimpleTrans.SameGender");
		if (reason.Contains(val.Resolve()))
		{
			Pawn val2 = (SimpleTransPregnancyUtility.CanSire(first) ? first : (SimpleTransPregnancyUtility.CanSire(second) ? second : null));
			Pawn obj = (SimpleTransPregnancyUtility.CanCarry(second) ? second : (SimpleTransPregnancyUtility.CanCarry(first) ? first : null));
			__result = true;
			if (val2 == null)
			{
				val = TranslatorFormattedStringExtensions.Translate("SimpleTrans.PawnsCannotSireChild", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
				__result = val.Resolve();
			}
			if (obj == null)
			{
				val = TranslatorFormattedStringExtensions.Translate("SimpleTrans.PawnsCannotCarryChild", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
				__result = val.Resolve();
			}
			bool flag = StatExtension.GetStatValue((Thing)(object)first, StatDefOf.Fertility, true, -1) <= 0f;
			bool flag2 = StatExtension.GetStatValue((Thing)(object)second, StatDefOf.Fertility, true, -1) <= 0f;
			if (flag && flag2)
			{
				val = TranslatorFormattedStringExtensions.Translate("PawnsAreInfertile", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
				__result = val.Resolve();
			}
			if (flag != flag2)
			{
				val = TranslatorFormattedStringExtensions.Translate("PawnIsInfertile", NamedArgumentUtility.Named((object)(flag ? val2 : second), "PAWN"));
				__result = val.Resolve();
			}
			bool flag3 = !first.ageTracker.CurLifeStage.reproductive;
			bool flag4 = !second.ageTracker.CurLifeStage.reproductive;
			if (flag3 && flag4)
			{
				val = TranslatorFormattedStringExtensions.Translate("PawnsAreTooYoung", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
				__result = val.Resolve();
			}
			if (flag3 != flag4)
			{
				val = TranslatorFormattedStringExtensions.Translate("PawnIsTooYoung", NamedArgumentUtility.Named((object)(flag3 ? first : second), "PAWN"));
				__result = val.Resolve();
			}
			bool flag5 = second.Sterile() && PregnancyUtility.GetPregnancyHediff(second) == null;
			bool flag6 = first.Sterile();
			if (flag6 && flag5)
			{
				val = TranslatorFormattedStringExtensions.Translate("PawnsAreSterile", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
				__result = val.Resolve();
			}
			if (flag6 != flag5)
			{
				val = TranslatorFormattedStringExtensions.Translate("PawnIsSterile", NamedArgumentUtility.Named((object)(flag6 ? first : second), "PAWN"));
				__result = val.Resolve();
			}
		}
		else if (__result)
		{
			Pawn obj2 = (SimpleTransPregnancyUtility.CanSire(first) ? first : (SimpleTransPregnancyUtility.CanSire(second) ? second : null));
			Pawn val3 = (SimpleTransPregnancyUtility.CanCarry(second) ? second : (SimpleTransPregnancyUtility.CanCarry(first) ? first : null));
			if (obj2 == null)
			{
				val = TranslatorFormattedStringExtensions.Translate("SimpleTrans.PawnsCannotSireChild", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
				__result = val.Resolve();
			}
			if (val3 == null)
			{
				val = TranslatorFormattedStringExtensions.Translate("SimpleTrans.PawnsCannotCarryChild", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
				__result = val.Resolve();
			}
		}
	}
}
