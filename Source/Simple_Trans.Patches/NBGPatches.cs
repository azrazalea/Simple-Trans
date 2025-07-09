using System;
using System.Reflection;
using HarmonyLib;
using NonBinaryGender;
using VEF.Genes;
using Verse;

namespace Simple_Trans.Patches;

/// <summary>
/// Harmony patches for Non-Binary Gender mod integration
/// Handles special cases for non-binary pawns with Simple Trans
/// </summary>
public static class NBGPatches
{
	#region Harmony Patch Setup
	/// <summary>
	/// Extension method to apply Non-Binary Gender specific patches
	/// </summary>
	/// <param name="harmony">The Harmony instance to use for patching</param>
	public static void PatchNBG(this Harmony harmony)
	{
		// Patch IsEnby to return EnbyUtility.IsEnby(pawn) instead of false
		harmony.Patch(
			(MethodBase)typeof(SimpleTransPregnancyUtility).GetMethod("IsEnby"),
			(HarmonyMethod)null,
			new HarmonyMethod(IsEnbyPostfix),
			(HarmonyMethod)null,
			(HarmonyMethod)null);
	}

	#endregion

	#region Patch Methods

	/// <summary>
	/// Postfix patch for IsEnby to return EnbyUtility.IsEnby(pawn) instead of false
	/// </summary>
	/// <param name="__result">The result from the original method</param>
	/// <param name="pawn">The pawn being evaluated</param>
	public static void IsEnbyPostfix(ref bool __result, Pawn pawn)
	{
		__result = EnbyUtility.IsEnby(pawn);
	}

	#endregion
}
