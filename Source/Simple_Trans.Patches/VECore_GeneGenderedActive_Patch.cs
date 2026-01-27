using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using VEF.Genes;
using Verse;

namespace Simple_Trans.Patches;

/// <summary>
/// Harmony prefix patch for VEF GeneGendered.Active getter
/// 
/// Purpose: Prevents gendered genes from activating on transgender pawns
/// 
/// Background: Vanilla Expanded Framework includes gendered genes that should only activate
/// based on biological sex. Simple Trans introduces gender identity that may differ
/// from biological sex. This patch ensures that gendered genes only activate for
/// cisgender pawns (where gender identity matches biological sex).
/// 
/// Implementation: Simple prefix check that returns false if the pawn is not cisgender,
/// preventing the gene from being active on transgender pawns.
/// </summary>
[HarmonyPatch(typeof(GeneGendered), "Active", MethodType.Getter)]
public static class VECore_GeneGenderedActive_Patch
{
	/// <summary>
	/// Prefix patch that prevents gendered genes from activating on transgender pawns
	/// </summary>
	/// <param name="__instance">The GeneGendered instance</param>
	/// <param name="__result">The result to return</param>
	/// <returns>True to continue to original method, false to skip it</returns>
	[HarmonyPrefix]
	public static bool Prefix(GeneGendered __instance, ref bool __result)
	{
		try
		{
			// If pawn is null, let original method handle it
			if (__instance?.pawn == null) return true;
			
			// Check if pawn is cisgender
			bool isCisgender = SimpleTransHediffs.IsCisgender(__instance.pawn);
			
			// If not cisgender, gene should not be active
			if (!isCisgender)
			{
				__result = false;
				return false; // Skip original method
			}
			
			// If cisgender, let original method determine activation
			return true;
		}
		catch (System.Exception ex)
		{
			SimpleTransDebug.Log($"Error in VECore GeneGendered prefix: {ex.Message}", 1);
			return true; // Let original method run on error
		}
	}
}
