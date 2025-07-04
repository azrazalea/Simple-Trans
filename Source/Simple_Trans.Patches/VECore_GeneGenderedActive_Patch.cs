using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using VEF.Genes;
using Verse;

namespace Simple_Trans.Patches;

[HarmonyPatch(typeof(GeneGendered), "Active", MethodType.Getter)]
public static class VECore_GeneGenderedActive_Patch
{
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected O, but got Unknown
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Expected O, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Expected O, but got Unknown
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Expected O, but got Unknown
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Expected O, but got Unknown
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Expected O, but got Unknown
		int num = -1;
		int num2 = -1;
		int num3 = 0;
		List<CodeInstruction> list = new List<CodeInstruction>(instructions);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].opcode == OpCodes.Br_S)
			{
				num3++;
				if (num3 > 2)
				{
					num2 = num;
					break;
				}
				num = i;
			}
		}
		if (num2 > -1)
		{
			list[num2].opcode = OpCodes.Brfalse_S;
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
			list.InsertRange(num2, list2);
		}
		else
		{
			Log.Error("Failed to transpile VECore GeneGendered.get_Active");
		}
		return list.AsEnumerable();
	}
}
