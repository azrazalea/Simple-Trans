using HarmonyLib;
using RimWorld;
using Verse;

namespace Simple_Trans;

/// <summary>
/// Harmony patch for PregnancyUtility.CanEverProduceChild
/// Allows same-gender pairs to produce children if they have compatible reproductive capabilities
/// </summary>
[HarmonyPatch(typeof(PregnancyUtility), "CanEverProduceChild")]
public class PregnancyUtility_CanEverProduceChild_Patch
{
	/// <summary>
	/// Postfix method to handle same-gender reproduction and verify reproductive capabilities
	/// </summary>
	/// <param name="__result">The result from the original method</param>
	/// <param name="first">First pawn in the pair</param>
	/// <param name="second">Second pawn in the pair</param>
	public static void Postfix(ref AcceptanceReport __result, Pawn first, Pawn second)
	{
		string reason = __result.Reason;
		TaggedString sameGenderText = Translator.Translate("SimpleTrans.SameGender");
		
		// Handle originally rejected same-gender pairs
		if (reason.Contains(sameGenderText.Resolve()))
		{
			HandleSameGenderPair(ref __result, first, second);
		}
		// Verify reproductive capabilities for originally accepted pairs
		else if (__result)
		{
			VerifyReproductiveCapabilities(ref __result, first, second);
		}
	}
	
	/// <summary>
	/// Handles same-gender pairs by checking if they have compatible reproductive capabilities
	/// </summary>
	/// <param name="result">The acceptance report to modify</param>
	/// <param name="first">First pawn in the pair</param>
	/// <param name="second">Second pawn in the pair</param>
	private static void HandleSameGenderPair(ref AcceptanceReport result, Pawn first, Pawn second)
	{
		Pawn sirer = (SimpleTransPregnancyUtility.CanSire(first) ? first : (SimpleTransPregnancyUtility.CanSire(second) ? second : null));
		Pawn carrier = (SimpleTransPregnancyUtility.CanCarry(second) ? second : (SimpleTransPregnancyUtility.CanCarry(first) ? first : null));
		
		// Initially accept the pair
		result = true;
		
		// Check for reproductive capability issues
		if (sirer == null)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("SimpleTrans.PawnsCannotSireChild", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
			result = message.Resolve();
			return;
		}
		
		if (carrier == null)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("SimpleTrans.PawnsCannotCarryChild", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
			result = message.Resolve();
			return;
		}
		
		// Check fertility issues
		CheckFertilityIssues(ref result, first, second, sirer);
		
		// Check age issues
		CheckAgeIssues(ref result, first, second);
		
		// Check sterility issues
		CheckSterilityIssues(ref result, first, second);
	}
	
	/// <summary>
	/// Verifies that already-accepted pairs have the necessary reproductive capabilities
	/// </summary>
	/// <param name="result">The acceptance report to modify</param>
	/// <param name="first">First pawn in the pair</param>
	/// <param name="second">Second pawn in the pair</param>
	private static void VerifyReproductiveCapabilities(ref AcceptanceReport result, Pawn first, Pawn second)
	{
		Pawn sirer = (SimpleTransPregnancyUtility.CanSire(first) ? first : (SimpleTransPregnancyUtility.CanSire(second) ? second : null));
		Pawn carrier = (SimpleTransPregnancyUtility.CanCarry(second) ? second : (SimpleTransPregnancyUtility.CanCarry(first) ? first : null));
		
		if (sirer == null)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("SimpleTrans.PawnsCannotSireChild", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
			result = message.Resolve();
		}
		else if (carrier == null)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("SimpleTrans.PawnsCannotCarryChild", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
			result = message.Resolve();
		}
	}
	
	/// <summary>
	/// Checks for fertility issues between pawns
	/// </summary>
	/// <param name="result">The acceptance report to modify</param>
	/// <param name="first">First pawn in the pair</param>
	/// <param name="second">Second pawn in the pair</param>
	/// <param name="sirer">The pawn that can sire</param>
	private static void CheckFertilityIssues(ref AcceptanceReport result, Pawn first, Pawn second, Pawn sirer)
	{
		bool firstIsInfertile = StatExtension.GetStatValue((Thing)(object)first, StatDefOf.Fertility, true, -1) <= 0f;
		bool secondIsInfertile = StatExtension.GetStatValue((Thing)(object)second, StatDefOf.Fertility, true, -1) <= 0f;
		
		if (firstIsInfertile && secondIsInfertile)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("PawnsAreInfertile", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
			result = message.Resolve();
		}
		else if (firstIsInfertile != secondIsInfertile)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("PawnIsInfertile", NamedArgumentUtility.Named((object)(firstIsInfertile ? sirer : second), "PAWN"));
			result = message.Resolve();
		}
	}
	
	/// <summary>
	/// Checks for age-related reproduction issues
	/// </summary>
	/// <param name="result">The acceptance report to modify</param>
	/// <param name="first">First pawn in the pair</param>
	/// <param name="second">Second pawn in the pair</param>
	private static void CheckAgeIssues(ref AcceptanceReport result, Pawn first, Pawn second)
	{
		bool firstIsTooYoung = !first.ageTracker.CurLifeStage.reproductive;
		bool secondIsTooYoung = !second.ageTracker.CurLifeStage.reproductive;
		
		if (firstIsTooYoung && secondIsTooYoung)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("PawnsAreTooYoung", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
			result = message.Resolve();
		}
		else if (firstIsTooYoung != secondIsTooYoung)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("PawnIsTooYoung", NamedArgumentUtility.Named((object)(firstIsTooYoung ? first : second), "PAWN"));
			result = message.Resolve();
		}
	}
	
	/// <summary>
	/// Checks for sterility issues between pawns
	/// </summary>
	/// <param name="result">The acceptance report to modify</param>
	/// <param name="first">First pawn in the pair</param>
	/// <param name="second">Second pawn in the pair</param>
	private static void CheckSterilityIssues(ref AcceptanceReport result, Pawn first, Pawn second)
	{
		bool firstIsSterile = first.Sterile();
		bool secondIsSterile = second.Sterile() && PregnancyUtility.GetPregnancyHediff(second) == null;
		
		if (firstIsSterile && secondIsSterile)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("PawnsAreSterile", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
			result = message.Resolve();
		}
		else if (firstIsSterile != secondIsSterile)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("PawnIsSterile", NamedArgumentUtility.Named((object)(firstIsSterile ? first : second), "PAWN"));
			result = message.Resolve();
		}
	}
}
