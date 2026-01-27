using System;
using HarmonyLib;
using Verse;

namespace Simple_Trans;

/// <summary>
/// Harmony patch for PawnGenerator.GeneratePawn
/// Ensures all newly generated pawns have their gender identity validated
/// </summary>
[HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
public class PawnGenerator_GeneratePawn_Patch
{
	/// <summary>
	/// Postfix method called after pawn generation
	/// Validates or sets gender for the newly created pawn
	/// </summary>
	/// <param name="__result">The generated pawn</param>
	public static void Postfix(ref Pawn __result)
	{
		SimpleTransCore.ValidateOrSetGender(__result);
	}
}
