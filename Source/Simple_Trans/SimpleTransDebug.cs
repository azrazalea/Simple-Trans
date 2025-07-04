using LudeonTK;
using RimWorld;
using Verse;

namespace Simple_Trans;

internal class SimpleTransDebug
{
	[DebugAction("Simple Trans", "Validate All Genders", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void ValidateAllHumanlikeGenders()
	{
		foreach (Pawn item in PawnsFinder.All_AliveOrDead)
		{
			SimpleTransPregnancyUtility.ValidateOrSetGender(item);
		}
	}

	[DebugAction("Simple Trans", "Set Transgender", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void SetTransgender(Pawn pawn)
	{
		SimpleTransPregnancyUtility.SetTrans(pawn);
	}

	[DebugAction("Simple Trans", "Set Cisgender", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void SetCisgender(Pawn pawn)
	{
		SimpleTransPregnancyUtility.SetCis(pawn);
	}

	[DebugAction("Simple Trans", "Set Able to Carry", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void SetAbleCarry(Pawn pawn)
	{
		SimpleTransPregnancyUtility.SetCarry(pawn, removeSire: false);
	}

	[DebugAction("Simple Trans", "Set Able to Sire", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void SetAbleSire(Pawn pawn)
	{
		SimpleTransPregnancyUtility.SetSire(pawn, removeCarry: false);
	}

	[DebugAction("Simple Trans", "Reset Gender", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void ResetGender(Pawn pawn)
	{
		SimpleTransPregnancyUtility.ClearGender(pawn);
		SimpleTransPregnancyUtility.ValidateOrSetGender(pawn);
	}

	[DebugAction("Simple Trans", "Clear Gender", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void ClearGender(Pawn pawn)
	{
		SimpleTransPregnancyUtility.ClearGender(pawn);
	}

	public static void Log(string message, int level = 1)
	{
		// Only log if debug mode is enabled
		if (SimpleTrans.debugMode && level <= 3)
		{
			Verse.Log.Message($"[SimpleTrans] {message}");
		}
	}
}
