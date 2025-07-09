using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Simple_Trans;

/// <summary>
/// Simplified transpiler that replaces the entire Biotech pregnancy block with capability-based logic
/// This is much cleaner than trying to surgically replace individual parts
/// </summary>
public static class PregnancyApplicationPatches
{
	/// <summary>
	/// Initializes all pregnancy-related patches for various mods
	/// </summary>
	public static void TryPatchAllPregnancySystems(Harmony harmony)
	{
		// Patch vanilla JobDriver_Lovin to fix pregnancy assignment
		TryPatchVanillaMakeNewToils(harmony);

		// Patch Intimacy mod
		IntimacyModTranspilers.TryPatchIntimacyMod(harmony);
	}

	/// <summary>
	/// Patches vanilla JobDriver_Lovin.MakeNewToils to replace the entire Biotech pregnancy block
	/// </summary>
	private static void TryPatchVanillaMakeNewToils(Harmony harmony)
	{
		try
		{
			// Find the actual compiler-generated methods that contain the pregnancy logic
			var lovinMethods = AccessTools.GetDeclaredMethods(typeof(JobDriver_Lovin))
				.Where(m => m.Name.Contains("<MakeNewToils>") && m.ReturnType == typeof(void))
				.ToList();

			SimpleTransDebug.Log($"Patching {lovinMethods.Count} JobDriver_Lovin methods for pregnancy capability", 2);

			foreach (var method in lovinMethods)
			{
				harmony.Patch(method, transpiler: new HarmonyMethod(typeof(PregnancyApplicationPatches), nameof(TranspileVanillaMakeNewToils)));
			}
		}
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error patching vanilla MakeNewToils: {ex}");
		}
	}

	/// <summary>
	/// Transpiler that replaces the entire Biotech pregnancy block with capability-based logic
	/// Much cleaner than trying to surgically replace individual parts
	/// </summary>
	public static IEnumerable<CodeInstruction> TranspileVanillaMakeNewToils(IEnumerable<CodeInstruction> instructions)
	{
		var codes = instructions.ToList();
		bool patched = false;

		try
		{
			// First, check if this method contains pregnancy code
			bool hasPregnancyCode = false;
			for (int i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo method)
				{
					if (method.Name == "get_BiotechActive" && method.DeclaringType?.Name == "ModsConfig")
					{
						hasPregnancyCode = true;
						break;
					}
				}
			}

			if (!hasPregnancyCode)
			{
				return codes.AsEnumerable();
			}

			// Look for the ModsConfig.BiotechActive check and replace everything inside it
			for (int i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo method)
				{
					// Find the ModsConfig.BiotechActive call
					if (method.Name == "get_BiotechActive" && method.DeclaringType?.Name == "ModsConfig")
					{
						// Find the start of the if block (after the branch instruction)
						int blockStart = -1;
						for (int j = i + 1; j < Math.Min(i + 10, codes.Count); j++)
						{
							if (codes[j].opcode == OpCodes.Brfalse || codes[j].opcode == OpCodes.Brfalse_S)
							{
								blockStart = j + 1; // Start after the branch
								break;
							}
						}

						if (blockStart == -1)
						{
							continue;
						}

						// Find the end of the if block by looking for branch targets or reasonable endpoints
						int blockEnd = -1;
						Label? branchTarget = null;

						// Get the branch target from the brfalse instruction
						for (int j = i + 1; j < Math.Min(i + 10, codes.Count); j++)
						{
							if ((codes[j].opcode == OpCodes.Brfalse || codes[j].opcode == OpCodes.Brfalse_S) && codes[j].operand is Label label)
							{
								branchTarget = label;
								break;
							}
						}

						// Find where the branch target is
						if (branchTarget.HasValue)
						{
							for (int j = blockStart; j < codes.Count; j++)
							{
								if (codes[j].labels?.Contains(branchTarget.Value) == true)
								{
									blockEnd = j - 1;
									break;
								}
							}
						}

						// If we couldn't find the exact end, use heuristics
						if (blockEnd <= blockStart)
						{
							for (int j = blockStart; j < Math.Min(blockStart + 100, codes.Count); j++)
							{
								// Look for end patterns - return, major branch, or other control flow
								if (codes[j].opcode == OpCodes.Ret ||
									codes[j].opcode == OpCodes.Br ||
									codes[j].opcode == OpCodes.Br_S ||
									(codes[j].labels?.Count > 0 && j > blockStart + 20))
								{
									blockEnd = j - 1;
									break;
								}
							}
						}

						// Final fallback
						if (blockEnd <= blockStart)
						{
							blockEnd = Math.Min(blockStart + 50, codes.Count - 1);
						}

						// Replace the entire block with our capability-based pregnancy logic
						var replacementMethod = AccessTools.Method(typeof(PregnancyApplicationPatches), nameof(HandleCapabilityBasedPregnancyFromJobDriver));
						var replacementInstructions = new List<CodeInstruction>
						{
							new CodeInstruction(OpCodes.Ldarg_0), // this (JobDriver_Lovin)
							new CodeInstruction(OpCodes.Call, replacementMethod) // HandleCapabilityBasedPregnancyFromJobDriver(this)
						};

						// Remove the original block
						int removeCount = blockEnd - blockStart + 1;
						if (blockStart >= 0 && blockStart + removeCount <= codes.Count && removeCount > 0)
						{
							codes.RemoveRange(blockStart, removeCount);
							codes.InsertRange(blockStart, replacementInstructions);
							patched = true;
						}

						break; // Only replace the first Biotech block we find
					}
				}
			}

			SimpleTransDebug.Log($"Vanilla pregnancy transpiler applied: {patched}", 2);
		}
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error in vanilla MakeNewToils transpiler: {ex}");
		}

		return codes.AsEnumerable();
	}

	/// <summary>
	/// Handles capability-based pregnancy from JobDriver_Lovin
	/// This completely replaces vanilla's gender-based pregnancy logic
	/// </summary>
	public static void HandleCapabilityBasedPregnancyFromJobDriver(JobDriver_Lovin jobDriver)
	{
		try
		{
			if (jobDriver?.pawn == null)
			{
				return;
			}

			// Extract partner using the same method as vanilla
			Pawn partner = jobDriver.job?.GetTarget(TargetIndex.A).Pawn;
			if (partner == null || !ModsConfig.BiotechActive)
			{
				return;
			}

			SimpleTransDebug.Log($"Lovin pregnancy attempt: {jobDriver.pawn.Name?.ToStringShort} + {partner.Name?.ToStringShort}", 2);

			// Use shared capability-based pregnancy logic with vanilla's pregnancy chance
			HandleCapabilityBasedPregnancy(jobDriver.pawn, partner, 0.05f);
		}
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error in capability-based pregnancy handler: {ex}");
		}
	}

	/// <summary>
	/// Shared capability-based pregnancy logic used by both vanilla and modded systems
	/// This replaces the problematic gender-based pawn assignment and null checks
	/// </summary>
	public static void HandleCapabilityBasedPregnancy(Pawn pawn1, Pawn pawn2, float baseChance)
	{
		try
		{
			if (!ModsConfig.BiotechActive)
			{
				return;
			}

			// Use unified pregnancy method that handles capability-based role assignment and chance calculation
			bool success = SimpleTransPregnancyUtility.TryCreatePregnancy(pawn1, pawn2, baseChance, showIncompatibilityMessage: true);
			
			if (success)
			{
				SimpleTransDebug.Log($"Pregnancy created: {pawn1?.Name?.ToStringShort} + {pawn2?.Name?.ToStringShort}", 1);
			}
		}
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error in capability-based pregnancy: {ex}");
		}
	}
}

/// <summary>
/// Conditional transpiler patches for Intimacy mod compatibility
/// This class is only processed if the Intimacy mod is loaded
/// </summary>
public static class IntimacyModTranspilers
{
	/// <summary>
	/// Initializes Intimacy mod patches if the mod is detected
	/// </summary>
	public static void TryPatchIntimacyMod(Harmony harmony)
	{
		if (!ModsConfig.IsActive("LovelyDovey.Sex.WithEuterpe"))
		{
			return;
		}

		try
		{
			Type jobDriverSexLeadType = AccessTools.TypeByName("LoveyDoveySexWithEuterpe.JobDriver_SexLead");
			if (jobDriverSexLeadType != null)
			{
				var method = AccessTools.Method(jobDriverSexLeadType, "TryHandlePregnancy");
				if (method != null)
				{
					harmony.Patch(method, prefix: new HarmonyMethod(typeof(IntimacyModTranspilers), nameof(PrefixIntimacyPregnancy)));

					SimpleTransDebug.Log("Intimacy mod pregnancy patched successfully", 2);
				}
				else
				{
					Log.Warning("[Simple Trans] Could not find Intimacy mod TryHandlePregnancy method");
				}
			}
			else
			{
				Log.Warning("[Simple Trans] Could not find Intimacy mod JobDriver_SexLead type");
			}
		}
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error patching Intimacy mod: {ex}");
		}
	}

	/// <summary>
	/// Prefix patch that replaces Intimacy mod's gender-based pregnancy logic with capability-based logic
	/// Returns false to skip the original method entirely
	/// </summary>
	public static bool PrefixIntimacyPregnancy(Pawn initiator, Pawn partner)
	{
		// Run our capability-based pregnancy logic
		HandleIntimacyCapabilityBasedPregnancy(initiator, partner);

		// Return false to skip the original gender-based method
		return false;
	}

	/// <summary>
	/// Replacement pregnancy logic for Intimacy mod using shared capability-based logic
	/// </summary>
	public static void HandleIntimacyCapabilityBasedPregnancy(Pawn initiator, Pawn partner)
	{
		SimpleTransDebug.Log($"Intimacy pregnancy attempt: {initiator?.Name?.ToStringShort} + {partner?.Name?.ToStringShort}", 2);

		// Use the same shared logic as vanilla, with Intimacy's base chance
		PregnancyApplicationPatches.HandleCapabilityBasedPregnancy(initiator, partner, 0.05f);
	}
}
