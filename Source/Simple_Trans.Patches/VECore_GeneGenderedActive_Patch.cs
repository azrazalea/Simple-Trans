using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using VEF.Genes;
using Verse;

namespace Simple_Trans.Patches;

/// <summary>
/// Harmony transpiler patch for VEF GeneGendered.Active getter
/// 
/// Purpose: Modifies gendered gene activation to respect Simple Trans cisgender status
/// 
/// Background: Vanilla Expanded Framework includes gendered genes that should only activate
/// based on biological sex. However, Simple Trans introduces gender identity that may differ
/// from biological sex. This transpiler ensures that gendered genes only activate for
/// cisgender pawns (where gender identity matches biological sex).
/// 
/// Implementation: Injects IL code to check for the "Cisgender" hediff before allowing
/// gender-specific gene activation. This prevents trans pawns from having their gendered
/// genes activate inappropriately (e.g., preventing beard genes on trans women).
/// 
/// Technical Approach: Uses IL transpiler to insert hediff checks into the compiled
/// GeneGendered.Active property getter, modifying the boolean logic to require both
/// the original gender check AND cisgender status.
/// </summary>
[HarmonyPatch(typeof(GeneGendered), "Active", MethodType.Getter)]
public static class VECore_GeneGenderedActive_Patch
{
	/// <summary>
	/// Transpiler that modifies the GeneGendered.Active getter to check for cisgender status
	/// This ensures gendered genes only activate for cisgender pawns
	/// </summary>
	/// <param name="instructions">The original IL instructions</param>
	/// <returns>Modified IL instructions</returns>
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		try
		{
			if (instructions == null)
			{
				Log.Error("[Simple Trans] VECore GeneGendered transpiler received null instructions");
				return new List<CodeInstruction>();
			}
			
			int lastBranchIndex = -1;
			int insertionIndex = -1;
			int branchCount = 0;
			List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
			
			if (instructionList.Count == 0)
			{
				Log.Error("[Simple Trans] VECore GeneGendered transpiler received empty instruction list");
				return instructionList;
			}
			
			// Find the appropriate insertion point by analyzing IL branch patterns
			// The original GeneGendered.Active getter has specific branch instructions we need to modify
			// We count branches to find the exact location where gender checks occur
			for (int i = 0; i < instructionList.Count; i++)
			{
				if (instructionList[i]?.opcode == OpCodes.Br_S)
				{
					branchCount++;
					// After the second branch, we've found our insertion point
					// This corresponds to the location where vanilla gender validation occurs
					if (branchCount > 2)
					{
						insertionIndex = lastBranchIndex;
						break;
					}
					lastBranchIndex = i;
				}
			}
			
			// Apply the transpiler modifications
			if (insertionIndex > -1)
			{
				ApplyTranspilerModifications(instructionList, insertionIndex);
				SimpleTransDebug.Log($"Successfully applied VECore GeneGendered transpiler at index {insertionIndex}", 3);
			}
			else
			{
				Log.Error("[Simple Trans] Failed to transpile VECore GeneGendered.get_Active - could not find insertion point");
			}
			
			return instructionList.AsEnumerable();
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Critical error in VECore GeneGendered transpiler: {ex}");
			// Return original instructions on failure to avoid breaking the game
			return instructions ?? new List<CodeInstruction>();
		}
	}
	
	/// <summary>
	/// Applies the actual IL code modifications at the specified insertion point
	/// </summary>
	/// <param name="instructionList">The instruction list to modify</param>
	/// <param name="insertionIndex">The index where to insert new instructions</param>
	private static void ApplyTranspilerModifications(List<CodeInstruction> instructionList, int insertionIndex)
	{
		try
		{
			if (instructionList == null)
			{
				Log.Error("[Simple Trans] ApplyTranspilerModifications called with null instruction list");
				return;
			}
			
			if (insertionIndex < 0 || insertionIndex >= instructionList.Count)
			{
				Log.Error($"[Simple Trans] Invalid insertion index {insertionIndex} for instruction list of length {instructionList.Count}");
				return;
			}
			
			// Change the branch instruction to conditional
			instructionList[insertionIndex].opcode = OpCodes.Brfalse_S;
			
			// Validate required reflection methods exist
			var geneField = AccessTools.Field(typeof(Gene), "pawn");
			var healthField = AccessTools.Field(typeof(Pawn), "health");
			var hediffSetField = AccessTools.Field(typeof(Pawn_HealthTracker), "hediffSet");
			var namedMethod = AccessTools.Method(typeof(HediffDef), "Named", null, null);
			var hasHediffMethod = AccessTools.Method(typeof(HediffSet), "HasHediff", new Type[2] { typeof(HediffDef), typeof(bool) }, null);
			
			if (geneField == null || healthField == null || hediffSetField == null || namedMethod == null || hasHediffMethod == null)
			{
				Log.Error("[Simple Trans] Failed to resolve required reflection members for transpiler");
				return;
			}
			
			// Create IL instructions to inject cisgender check into the gene activation logic
			// This generates the equivalent C# code: this.pawn.health.hediffSet.HasHediff(HediffDef.Named("Cisgender"), false)
			// The injection ensures that gendered genes only activate for cisgender pawns
			List<CodeInstruction> injectedInstructions = new List<CodeInstruction>
			{
				// Load 'this' (the gene instance) - equivalent to 'this'
				new CodeInstruction(OpCodes.Ldarg_0, null),
				// Load pawn field from gene - equivalent to 'this.pawn'
				new CodeInstruction(OpCodes.Ldfld, geneField),
				// Load health field from pawn - equivalent to 'this.pawn.health'
				new CodeInstruction(OpCodes.Ldfld, healthField),
				// Load hediffSet field from health tracker - equivalent to 'this.pawn.health.hediffSet'
				new CodeInstruction(OpCodes.Ldfld, hediffSetField),
				// Load "Cisgender" string constant onto stack
				new CodeInstruction(OpCodes.Ldstr, "Cisgender"),
				// Call HediffDef.Named("Cisgender") to get the hediff definition
				new CodeInstruction(OpCodes.Call, namedMethod),
				// Load false boolean (bodyPartsOnly parameter for HasHediff)
				new CodeInstruction(OpCodes.Ldc_I4_0, null),
				// Call HasHediff method - equivalent to hediffSet.HasHediff(cisgenderDef, false)
				new CodeInstruction(OpCodes.Callvirt, hasHediffMethod)
			};
			
			// Insert the new instructions
			instructionList.InsertRange(insertionIndex, injectedInstructions);
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error applying transpiler modifications: {ex}");
		}
	}
}
