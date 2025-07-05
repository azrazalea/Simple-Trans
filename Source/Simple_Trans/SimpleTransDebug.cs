using LudeonTK;
using RimWorld;
using Verse;

namespace Simple_Trans;

/// <summary>
/// Debug utilities and actions for Simple Trans mod
/// Provides development tools and logging functionality
/// </summary>
internal class SimpleTransDebug
{
	#region Debug Actions
	/// <summary>
	/// Validates gender for all pawns in the game (alive or dead)
	/// </summary>
	[DebugAction("Simple Trans", "Validate All Genders", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void ValidateAllHumanlikeGenders()
	{
		foreach (Pawn item in PawnsFinder.All_AliveOrDead)
		{
			SimpleTransPregnancyUtility.ValidateOrSetGender(item);
		}
	}

	/// <summary>
	/// Sets the selected pawn as transgender
	/// </summary>
	/// <param name="pawn">The pawn to modify</param>
	[DebugAction("Simple Trans", "Set Transgender", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void SetTransgender(Pawn pawn)
	{
		SimpleTransPregnancyUtility.SetTrans(pawn);
	}

	/// <summary>
	/// Sets the selected pawn as cisgender
	/// </summary>
	/// <param name="pawn">The pawn to modify</param>
	[DebugAction("Simple Trans", "Set Cisgender", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void SetCisgender(Pawn pawn)
	{
		SimpleTransPregnancyUtility.SetCis(pawn);
	}

	/// <summary>
	/// Grants the selected pawn exclusive pregnancy carrying ability
	/// </summary>
	/// <param name="pawn">The pawn to modify</param>
	[DebugAction("Simple Trans", "Set Able to Carry (exclusive)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void SetAbleCarry(Pawn pawn)
	{
		SimpleTransPregnancyUtility.SetCarry(pawn, removeSire: true);
	}

	/// <summary>
	/// Grants the selected pawn exclusive pregnancy siring ability
	/// </summary>
	/// <param name="pawn">The pawn to modify</param>
	[DebugAction("Simple Trans", "Set Able to Sire (exclusive)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void SetAbleSire(Pawn pawn)
	{
		SimpleTransPregnancyUtility.SetSire(pawn, removeCarry: true);
	}

	/// <summary>
	/// Grants the selected pawn both reproductive abilities (carry and sire)
	/// </summary>
	/// <param name="pawn">The pawn to modify</param>
	[DebugAction("Simple Trans", "Set Dual Reproductive", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void SetDualReproductive(Pawn pawn)
	{
		// Ensure pawn is transgender to allow dual capabilities
		if (!pawn.health.hediffSet.HasHediff(SimpleTransPregnancyUtility.transDef, false))
		{
			SimpleTransPregnancyUtility.SetTrans(pawn);
		}
		
		// Enable both reproductive abilities
		SimpleTransPregnancyUtility.SetCarry(pawn, removeSire: false);
		SimpleTransPregnancyUtility.SetSire(pawn, removeCarry: false);
	}

	/// <summary>
	/// Clears and resets gender for the selected pawn
	/// </summary>
	/// <param name="pawn">The pawn to reset</param>
	[DebugAction("Simple Trans", "Reset Gender", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void ResetGender(Pawn pawn)
	{
		SimpleTransPregnancyUtility.ClearGender(pawn);
		SimpleTransPregnancyUtility.ValidateOrSetGender(pawn);
	}

	/// <summary>
	/// Removes all gender-related hediffs from the selected pawn
	/// </summary>
	/// <param name="pawn">The pawn to clear</param>
	[DebugAction("Simple Trans", "Clear Gender", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void ClearGender(Pawn pawn)
	{
		SimpleTransPregnancyUtility.ClearGender(pawn);
	}
	
	#endregion

	#region Logging Utilities
	
	/// <summary>
	/// Logs a debug message if debug mode is enabled
	/// </summary>
	/// <param name="message">The message to log</param>
	/// <param name="level">The log level (1-3, lower is more important)</param>
	public static void Log(string message, int level = 1)
	{
		// Only log if debug mode is enabled
		if (SimpleTrans.debugMode && level <= 3)
		{
			Verse.Log.Message($"[SimpleTrans] {message}");
		}
	}
	
	#endregion
}
