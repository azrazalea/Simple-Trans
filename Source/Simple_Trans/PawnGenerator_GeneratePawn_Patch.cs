using System;
using HarmonyLib;
using Verse;

namespace Simple_Trans;

[HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
public class PawnGenerator_GeneratePawn_Patch
{
	public static void Postfix(ref Pawn __result)
	{
		SimpleTransPregnancyUtility.ValidateOrSetGender(__result);
	}
}
