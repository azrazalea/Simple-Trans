using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using VEF.Genes;
using Verse;

namespace Simple_Trans.Patches;

[HarmonyPatch(typeof(GeneUtils), "ApplyGeneEffects")]
public static class VECore_GeneUtilsApplyGeneEffects_Patch
{
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
	{
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Expected O, but got Unknown
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Expected O, but got Unknown
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Expected O, but got Unknown
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Expected O, but got Unknown
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Expected O, but got Unknown
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Expected O, but got Unknown
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Expected O, but got Unknown
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Expected O, but got Unknown
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Expected O, but got Unknown
		int num = -1;
		bool flag = false;
		List<CodeInstruction> list = new List<CodeInstruction>(instructions);
		Label? label = il.DefineLabel();
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].opcode == OpCodes.Ldloc_0 && i + 1 < list.Count && (FieldInfo)list[i + 1].operand == AccessTools.Field(typeof(GeneExtension), "forceFemale"))
			{
				num = i;
			}
			if (num > -1 && list[i].opcode == OpCodes.Ldsfld && i + 1 < list.Count && (FieldInfo)list[i].operand == AccessTools.Field(typeof(BodyTypeDefOf), "Female") && list[i + 1].opcode == OpCodes.Ceq)
			{
				flag = true;
			}
			if (flag && list[i].opcode == OpCodes.Brfalse_S)
			{
				CodeInstructionExtensions.Branches(list[i], out label);
				break;
			}
		}
		if (num > -1 && label.HasValue)
		{
			List<CodeInstruction> list2 = new List<CodeInstruction>();
			list2.Add(new CodeInstruction(OpCodes.Ldarg_0, (object)null));
			list2.Add(new CodeInstruction(OpCodes.Ldfld, (object)AccessTools.Field(typeof(Gene), "pawn")));
			list2.Add(new CodeInstruction(OpCodes.Ldfld, (object)AccessTools.Field(typeof(Pawn), "health")));
			list2.Add(new CodeInstruction(OpCodes.Ldfld, (object)AccessTools.Field(typeof(Pawn_HealthTracker), "hediffSet")));
			list2.Add(new CodeInstruction(OpCodes.Ldstr, (object)"Cisgender"));
			list2.Add(new CodeInstruction(OpCodes.Call, (object)AccessTools.Method(typeof(HediffDef), "Named", (Type[])null, (Type[])null)));
			list2.Add(new CodeInstruction(OpCodes.Ldc_I4_0, (object)null));
			list2.Add(new CodeInstruction(OpCodes.Callvirt, (object)AccessTools.Method(typeof(HediffSet), "HasHediff", new Type[2]
			{
				typeof(HediffDef),
				typeof(bool)
			}, (Type[])null)));
			list2.Add(new CodeInstruction(OpCodes.Brfalse_S, (object)label));
			list.InsertRange(num, list2);
		}
		else
		{
			Log.Error("Failed to transpile VECore GeneUtils.ApplyGeneEffects().");
		}
		return list.AsEnumerable();
	}
}
