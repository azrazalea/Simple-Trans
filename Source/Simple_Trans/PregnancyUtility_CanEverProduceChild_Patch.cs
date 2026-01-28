using System.Collections.Generic;
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
		// Override vanilla logic completely - check reproductive capabilities for ALL pairs
		// This ensures trans individuals, prosthetics, and custom reproductive setups work correctly

		CheckReproductiveCompatibility(ref __result, first, second);
	}

	/// <summary>
	/// Checks reproductive compatibility for all pawn pairs (replaces vanilla gender-based logic)
	/// Handles trans individuals, prosthetics, and custom reproductive configurations
	/// </summary>
	/// <param name="result">The acceptance report to modify</param>
	/// <param name="first">First pawn in the pair</param>
	/// <param name="second">Second pawn in the pair</param>
	private static void CheckReproductiveCompatibility(ref AcceptanceReport result, Pawn first, Pawn second)
	{
		// Validate input pawns
		if (first == null || second == null)
		{
			result = "One or both pawns are null";
			return;
		}

		// Check basic requirements (humanlike, alive, etc.)
		if (!first.RaceProps.Humanlike || !second.RaceProps.Humanlike)
		{
			result = "Both pawns must be humanlike";
			return;
		}

		// Determine reproductive roles using unified logic
		SimpleTransHediffs.DetermineRoles(first, second, out Pawn carrier, out Pawn sirer);

		// Check for reproductive capability requirements
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

		// Initially accept the pair - we have basic reproductive compatibility
		result = true;

		// Check fertility issues using capability-based fertility
		CheckFertilityIssues(ref result, carrier, sirer);
		if (!result.Accepted) return;

		// Check age issues
		CheckAgeIssues(ref result, first, second);
		if (!result.Accepted) return;

		// Check sterility issues isn't needed because it is baked into CanSire/CanCarry
	}


	/// <summary>
	/// Checks for fertility issues using capability-based fertility (correct age curves)
	/// </summary>
	/// <param name="result">The acceptance report to modify</param>
	/// <param name="carrier">The pawn who will carry</param>
	/// <param name="sirer">The pawn who will sire</param>
	private static void CheckFertilityIssues(ref AcceptanceReport result, Pawn carrier, Pawn sirer)
	{
		// Use capability-based fertility with correct age curves
		float carrierFertility = SimpleTransHediffs.GetCarryFertility(carrier);
		float sirerFertility = SimpleTransHediffs.GetSireFertility(sirer);
		bool carrierIsInfertile = carrierFertility <= 0f;
		bool sirerIsInfertile = sirerFertility <= 0f;

		if (carrierIsInfertile && sirerIsInfertile)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("PawnsAreInfertile", NamedArgumentUtility.Named((object)carrier, "PAWN1"), NamedArgumentUtility.Named((object)sirer, "PAWN2"));
			result = message.Resolve();
		}
		else if (carrierIsInfertile)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("PawnIsInfertile", NamedArgumentUtility.Named((object)carrier, "PAWN"));
			result = message.Resolve();
		}
		else if (sirerIsInfertile)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("PawnIsInfertile", NamedArgumentUtility.Named((object)sirer, "PAWN"));
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
}
