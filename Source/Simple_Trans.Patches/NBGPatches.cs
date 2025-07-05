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
		// Patch ValidateOrSetGenderWithGenes to handle non-binary pawns
		harmony.Patch(
			(MethodBase)typeof(SimpleTransPregnancyUtility).GetMethod("ValidateOrSetGenderWithGenes"), 
			new HarmonyMethod((Delegate)new Action<Pawn, Gene>(ValidateOrSetGenderWithGenesPrefix)), 
			(HarmonyMethod)null, 
			(HarmonyMethod)null, 
			(HarmonyMethod)null);
			
		// Patch SetCis to warn about non-binary pawns
		harmony.Patch(
			(MethodBase)typeof(SimpleTransPregnancyUtility).GetMethod("SetCis"), 
			new HarmonyMethod((Delegate)new Action<Pawn>(SetCisPrefix)), 
			(HarmonyMethod)null, 
			(HarmonyMethod)null, 
			(HarmonyMethod)null);
			
		// Patch DecideTransgender to consider non-binary pawns as transgender
		harmony.Patch(
			(MethodBase)typeof(SimpleTransPregnancyUtility).GetMethod("DecideTransgender"), 
			(HarmonyMethod)null, 
			new HarmonyMethod(DecideTransgenderPostfix), 
			(HarmonyMethod)null, 
			(HarmonyMethod)null);
	}
	
	#endregion

	#region Patch Methods

	/// <summary>
	/// Prefix patch for ValidateOrSetGenderWithGenes to handle non-binary pawns with gene extensions
	/// </summary>
	/// <param name="pawn">The pawn to process</param>
	/// <param name="gene">The gene being evaluated</param>
	public static void ValidateOrSetGenderWithGenesPrefix(Pawn pawn, Gene gene)
	{
		GeneExtension modExtension = ((Def)gene.def).GetModExtension<GeneExtension>();
		if (modExtension == null)
		{
			return;
		}
		
		// Handle forceFemale gene extension
		if (modExtension.forceFemale)
		{
			HandleForceFemaleGene(pawn);
		}
		
		// Handle forceMale gene extension
		if (modExtension.forceMale)
		{
			HandleForceMaleGene(pawn);
		}
	}

	/// <summary>
	/// Postfix patch for DecideTransgender to consider non-binary pawns as transgender
	/// </summary>
	/// <param name="__result">The result from the original method</param>
	/// <param name="pawn">The pawn being evaluated</param>
	public static void DecideTransgenderPostfix(ref bool __result, Pawn pawn)
	{
		// Non-binary pawns are considered transgender by default
		__result = __result || EnbyUtility.IsEnby(pawn);
	}

	/// <summary>
	/// Prefix patch for SetCis to warn about setting non-binary pawns as cisgender
	/// </summary>
	/// <param name="pawn">The pawn being set as cisgender</param>
	public static void SetCisPrefix(Pawn pawn)
	{
		if (EnbyUtility.IsEnby(pawn))
		{
			Log.Warning("[Simple Trans] Setting non-binary pawn to cisgender - this may cause unexpected behavior.");
		}
	}
	
	#endregion

	#region Helper Methods
	
	/// <summary>
	/// Handles forceFemale gene extension for non-binary pawns
	/// </summary>
	/// <param name="pawn">The pawn to process</param>
	private static void HandleForceFemaleGene(Pawn pawn)
	{
		// Only change gender if not already non-binary
		if (!EnbyUtility.IsEnby(pawn))
		{
			pawn.gender = (Rand.Range(0f, 1f) > SimpleTransPregnancyUtility.cisRate) ? Gender.Male : Gender.Female;
		}
		
		// Set gender identity based on assignment
		if (pawn.gender == Gender.Male || EnbyUtility.IsEnby(pawn))
		{
			SimpleTransPregnancyUtility.SetTrans(pawn);
		}
		else
		{
			SimpleTransPregnancyUtility.SetCis(pawn);
		}
		
		// Grant carrying ability
		SimpleTransPregnancyUtility.SetCarry(pawn, removeSire: false);
	}
	
	/// <summary>
	/// Handles forceMale gene extension for non-binary pawns
	/// </summary>
	/// <param name="pawn">The pawn to process</param>
	private static void HandleForceMaleGene(Pawn pawn)
	{
		// Only change gender if not already non-binary
		if (!EnbyUtility.IsEnby(pawn))
		{
			pawn.gender = (!(Rand.Range(0f, 1f) > SimpleTransPregnancyUtility.cisRate)) ? Gender.Male : Gender.Female;
		}
		
		// Set gender identity based on assignment
		if (pawn.gender == Gender.Female || EnbyUtility.IsEnby(pawn))
		{
			SimpleTransPregnancyUtility.SetTrans(pawn);
		}
		else
		{
			SimpleTransPregnancyUtility.SetCis(pawn);
		}
		
		// Grant siring ability
		SimpleTransPregnancyUtility.SetSire(pawn, removeCarry: false);
	}
	
	#endregion
}
