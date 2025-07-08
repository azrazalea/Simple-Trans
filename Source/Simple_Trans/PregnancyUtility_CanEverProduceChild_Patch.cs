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

		bool firstCanSire = SimpleTransPregnancyUtility.CanSire(first);
		bool firstCanCarry = SimpleTransPregnancyUtility.CanCarry(first);
		bool secondCanSire = SimpleTransPregnancyUtility.CanSire(second);
		bool secondCanCarry = SimpleTransPregnancyUtility.CanCarry(second);

		// Determine reproductive roles
		Pawn sirer = (firstCanSire ? first : (secondCanSire ? second : null));
		Pawn carrier = (secondCanCarry ? second : (firstCanCarry ? first : null));


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


		// Check fertility issues
		CheckFertilityIssues(ref result, first, second, sirer);
		if (!result.Accepted) return;

		// Check age issues
		CheckAgeIssues(ref result, first, second);
		if (!result.Accepted) return;

		// Check sterility issues
		CheckSterilityIssues(ref result, first, second);
		if (!result.Accepted) return;
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
		float firstFertility = StatExtension.GetStatValue((Thing)(object)first, StatDefOf.Fertility, true, -1);
		float secondFertility = StatExtension.GetStatValue((Thing)(object)second, StatDefOf.Fertility, true, -1);
		bool firstIsInfertile = firstFertility <= 0f;
		bool secondIsInfertile = secondFertility <= 0f;


		if (firstIsInfertile && secondIsInfertile)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("PawnsAreInfertile", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
			result = message.Resolve();
		}
		else if (firstIsInfertile != secondIsInfertile)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("PawnIsInfertile", NamedArgumentUtility.Named((object)(firstIsInfertile ? first : second), "PAWN"));
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
	/// Checks for capability-specific sterility issues between pawns
	/// </summary>
	/// <param name="result">The acceptance report to modify</param>
	/// <param name="first">First pawn in the pair</param>
	/// <param name="second">Second pawn in the pair</param>
	private static void CheckSterilityIssues(ref AcceptanceReport result, Pawn first, Pawn second)
	{
		// Determine who can carry and who can sire
		Pawn carrier = null;
		Pawn sirer = null;

		if (SimpleTransPregnancyUtility.CanCarry(first) && SimpleTransPregnancyUtility.CanSire(second))
		{
			carrier = first;
			sirer = second;
		}
		else if (SimpleTransPregnancyUtility.CanCarry(second) && SimpleTransPregnancyUtility.CanSire(first))
		{
			carrier = second;
			sirer = first;
		}
		else
		{
			// No reproductive compatibility - already handled by CheckReproductiveCompatibility
			return;
		}

		// Check if carrier is sterilized for carrying (but not pregnant)
		bool carrierIsSterilizedForCarrying = IsCarryingSterilized(carrier);
		bool carrierIsPregnant = PregnancyUtility.GetPregnancyHediff(carrier) != null;
		
		// Check if sirer is sterilized for siring
		bool sirerIsSterilizedForSiring = IsSiringSterilized(sirer);

		// If carrier is pregnant, they're not sterile for carrying purposes
		if (carrierIsPregnant)
		{
			carrierIsSterilizedForCarrying = false;
		}

		if (carrierIsSterilizedForCarrying && sirerIsSterilizedForSiring)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("PawnsAreSterile", NamedArgumentUtility.Named((object)first, "PAWN1"), NamedArgumentUtility.Named((object)second, "PAWN2"));
			result = message.Resolve();
		}
		else if (carrierIsSterilizedForCarrying)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("PawnIsSterile", NamedArgumentUtility.Named((object)carrier, "PAWN"));
			result = message.Resolve();
		}
		else if (sirerIsSterilizedForSiring)
		{
			TaggedString message = TranslatorFormattedStringExtensions.Translate("PawnIsSterile", NamedArgumentUtility.Named((object)sirer, "PAWN"));
			result = message.Resolve();
		}
	}

	/// <summary>
	/// Checks if a pawn is sterilized for carrying pregnancies
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>True if the pawn is sterilized for carrying</returns>
	private static bool IsCarryingSterilized(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null) return false;

		// Check for vanilla sterilization (blocks all reproduction)
		if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Sterilized) != null)
		{
			return true;
		}

		// Check for carry-specific sterilization
		var sterilizedCarryDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedCarry");
		if (sterilizedCarryDef != null && pawn.health.hediffSet.GetFirstHediffOfDef(sterilizedCarryDef) != null)
		{
			return true;
		}

		// Check for reversible carry sterilization
		var reversibleSterilizedCarryDef = DefDatabase<HediffDef>.GetNamedSilentFail("ReversibleSterilizedCarry");
		if (reversibleSterilizedCarryDef != null && pawn.health.hediffSet.GetFirstHediffOfDef(reversibleSterilizedCarryDef) != null)
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Checks if a pawn is sterilized for siring children
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>True if the pawn is sterilized for siring</returns>
	private static bool IsSiringSterilized(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null) return false;

		// Check for vanilla sterilization (blocks all reproduction)
		if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Sterilized) != null)
		{
			return true;
		}

		// Check for sire-specific sterilization
		var sterilizedSireDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedSire");
		if (sterilizedSireDef != null && pawn.health.hediffSet.GetFirstHediffOfDef(sterilizedSireDef) != null)
		{
			return true;
		}

		// Check for reversible sire sterilization
		var reversibleSterilizedSireDef = DefDatabase<HediffDef>.GetNamedSilentFail("ReversibleSterilizedSire");
		if (reversibleSterilizedSireDef != null && pawn.health.hediffSet.GetFirstHediffOfDef(reversibleSterilizedSireDef) != null)
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Creates a unique key for a pawn pair (order-independent)
	/// </summary>
	/// <param name="first">First pawn</param>
	/// <param name="second">Second pawn</param>
	/// <returns>Unique string key for the pair</returns>
	private static string GetPairKey(Pawn first, Pawn second)
	{
		if (first == null || second == null) return "null_pair";

		// Create order-independent key
		int firstId = first.thingIDNumber;
		int secondId = second.thingIDNumber;

		if (firstId < secondId)
			return $"{firstId}_{secondId}";
		else
			return $"{secondId}_{firstId}";
	}
}
