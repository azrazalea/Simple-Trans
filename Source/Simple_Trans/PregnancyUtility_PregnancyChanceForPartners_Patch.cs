using HarmonyLib;
using RimWorld;
using Verse;

namespace Simple_Trans;

[HarmonyPatch(typeof(PregnancyUtility), "PregnancyChanceForPartners")]
public class PregnancyUtility_PregnancyChanceForPartners_Patch
{
	public static void Postfix(ref float __result, Pawn woman, Pawn man)
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		Pawn val = (SimpleTransPregnancyUtility.CanCarry(woman) ? woman : (SimpleTransPregnancyUtility.CanCarry(man) ? man : null));
		Pawn val2 = (SimpleTransPregnancyUtility.CanSire(man) ? man : (SimpleTransPregnancyUtility.CanSire(woman) ? woman : null));
		float num = ((val2 == null) ? 1f : PregnancyUtility.PregnancyChanceForPawn(val2));
		float num2 = ((val == null || val == val2) ? 0f : PregnancyUtility.PregnancyChanceForWoman(val));
		float num3 = num * num2;
		float num4 = ((val != null) ? PregnancyUtility.GetPregnancyChanceFactor(val.relations.GetPregnancyApproachForPartner(val2)) : 0f);
		__result = num4 * num3;
	}
}
