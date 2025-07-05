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
/// Harmony transpiler patch for VEF GeneUtils.ApplyGeneEffects
/// Modifies gene effect application to respect cisgender status for body type changes
/// </summary>
[HarmonyPatch(typeof(GeneUtils), "ApplyGeneEffects")]
public static class VECore_GeneUtilsApplyGeneEffects_Patch
{
	/// <summary>
	/// Transpiler that modifies GeneUtils.ApplyGeneEffects to check for cisgender status
	/// This prevents body type changes from affecting transgender pawns
	/// </summary>
	/// <param name="instructions">The original IL instructions</param>
	/// <param name="il">The IL generator for creating labels</param>
	/// <returns>Modified IL instructions</returns>
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
	{
		int insertionIndex = -1;
		bool foundFemaleCheck = false;
		List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
		Label? branchLabel = il.DefineLabel();
		
		// Find the appropriate insertion point by locating the female body type check
		for (int i = 0; i < instructionList.Count; i++)
		{
			// Look for forceFemale field access
			if (instructionList[i].opcode == OpCodes.Ldloc_0 && 
				i + 1 < instructionList.Count && 
				(FieldInfo)instructionList[i + 1].operand == AccessTools.Field(typeof(GeneExtension), "forceFemale"))
			{
				insertionIndex = i;
			}
			
			// Look for Female body type comparison
			if (insertionIndex > -1 && 
				instructionList[i].opcode == OpCodes.Ldsfld && 
				i + 1 < instructionList.Count && 
				(FieldInfo)instructionList[i].operand == AccessTools.Field(typeof(BodyTypeDefOf), "Female") && 
				instructionList[i + 1].opcode == OpCodes.Ceq)
			{
				foundFemaleCheck = true;
			}
			
			// Find the branch instruction to get the target label
			if (foundFemaleCheck && instructionList[i].opcode == OpCodes.Brfalse_S)
			{
				CodeInstructionExtensions.Branches(instructionList[i], out branchLabel);
				break;
			}
		}
		
		// Apply the modifications if we found the insertion point
		if (insertionIndex > -1 && branchLabel.HasValue)
		{
			ApplyGeneEffectsModifications(instructionList, insertionIndex, branchLabel.Value);
		}
		else
		{
			Log.Error("[Simple Trans] Failed to transpile VECore GeneUtils.ApplyGeneEffects() - could not find insertion point");
		}
		
		return instructionList.AsEnumerable();
	}
	
	/// <summary>
	/// Applies the actual IL code modifications for the gene effects transpiler
	/// </summary>
	/// <param name="instructionList">The instruction list to modify</param>
	/// <param name="insertionIndex">The index where to insert new instructions</param>
	/// <param name="branchLabel">The label to branch to if cisgender check fails</param>
	private static void ApplyGeneEffectsModifications(List<CodeInstruction> instructionList, int insertionIndex, Label branchLabel)
	{
		// Create instructions to check for cisgender hediff
		List<CodeInstruction> injectedInstructions = new List<CodeInstruction>
		{
			// Load 'this' (the gene)
			new CodeInstruction(OpCodes.Ldarg_0, null),
			// Load pawn field
			new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Gene), "pawn")),
			// Load health field
			new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "health")),
			// Load hediffSet field
			new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_HealthTracker), "hediffSet")),
			// Load "Cisgender" string
			new CodeInstruction(OpCodes.Ldstr, "Cisgender"),
			// Call HediffDef.Named
			new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HediffDef), "Named", null, null)),
			// Load false boolean
			new CodeInstruction(OpCodes.Ldc_I4_0, null),
			// Call HasHediff method
			new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(HediffSet), "HasHediff", new Type[2]
			{
				typeof(HediffDef),
				typeof(bool)
			}, null)),
			// Branch if not cisgender (skip the body type change)
			new CodeInstruction(OpCodes.Brfalse_S, branchLabel)
		};
		
		// Insert the new instructions
		instructionList.InsertRange(insertionIndex, injectedInstructions);
	}
}
