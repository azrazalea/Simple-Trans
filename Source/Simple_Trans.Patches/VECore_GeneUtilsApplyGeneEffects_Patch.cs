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

/// <summary>
/// Harmony prefix patch for VEF GeneUtils.ApplyGeneEffects
/// Prevents gender-forcing genes from affecting transgender pawns
/// </summary>
[HarmonyPatch(typeof(GeneUtils), "ApplyGeneEffects")]
public static class VECore_GeneUtilsApplyGeneEffects_Patch
{
	/// <summary>
	/// Prefix patch that prevents gender-forcing gene effects on transgender pawns
	/// </summary>
	/// <param name="gene">The gene being processed</param>
	/// <returns>True to continue to original method, false to skip it</returns>
	[HarmonyPrefix]
	public static bool Prefix(Gene gene)
	{
		try
		{
			// If gene or pawn is null, let original method handle it
			if (gene?.pawn == null) return true;
			
			// Check if this gene has gender-forcing effects
			var extension = gene.def.GetModExtension<GeneExtension>();
			if (extension == null) return true; // No extension, proceed normally
			
			// If gene forces gender changes and pawn is not cisgender, skip the effects
			if ((extension.forceFemale == true || extension.forceMale == true))
			{
				bool isCisgender = SimpleTransHediffs.IsCisgender(gene.pawn);
				if (!isCisgender)
				{
					SimpleTransDebug.Log($"Skipping gender-forcing gene effects for transgender pawn {gene.pawn.Name}", 3);
					return false; // Skip original method entirely
				}
			}
			
			// For cisgender pawns or non-gender-forcing genes, proceed normally
			return true;
		}
		catch (System.Exception ex)
		{
			SimpleTransDebug.Log($"Error in VECore GeneUtils prefix: {ex.Message}", 1);
			return true; // Let original method run on error
		}
	}
}
