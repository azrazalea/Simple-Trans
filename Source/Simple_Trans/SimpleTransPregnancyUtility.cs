using System;
using System.Linq;
using RimWorld;
using VEF.Genes;
using Verse;
using XmlExtensions;

namespace Simple_Trans;

/// <summary>
/// Core utility class for Simple Trans mod functionality
/// Handles gender identity, reproductive capabilities, and related game mechanics
/// </summary>
public static class SimpleTransPregnancyUtility
{
	#region Static Configuration Fields

	/// <summary>
	/// Current cisgender rate (percentage of pawns that are cisgender)
	/// </summary>
	public static float cisRate;

	/// <summary>
	/// Rate at which trans men can carry pregnancies
	/// </summary>
	public static float transManCarryPercent;

	/// <summary>
	/// Rate at which trans women can sire offspring
	/// </summary>
	public static float transWomanSireRate;

	/// <summary>
	/// Rate at which non-binary pawns can carry pregnancies
	/// </summary>
	public static float enbyCarryRate;

	/// <summary>
	/// Whether genes represent assigned gender at birth (AGAB)
	/// </summary>
	public static bool genesAreAgab;

	/// <summary>
	/// Rate at which trans people have both abilities
	/// </summary>
	public static float transBothRate;

	/// <summary>
	/// Rate at which trans people have neither ability
	/// </summary>
	public static float transNeitherRate;

	/// <summary>
	/// Rate at which cis men can carry
	/// </summary>
	public static float cisManCarryRate;

	/// <summary>
	/// Rate at which cis women can sire
	/// </summary>
	public static float cisWomanSireRate;

	/// <summary>
	/// Rate at which cis people have both abilities
	/// </summary>
	public static float cisBothRate;

	/// <summary>
	/// Rate at which non-binary people have both abilities
	/// </summary>
	public static float enbyBothRate;

	/// <summary>
	/// Rate at which non-binary people have neither ability
	/// </summary>
	public static float enbyNeitherRate;

	/// <summary>
	/// Rate at which pawns are sterilized for carrying
	/// </summary>
	public static float carrySterilizationRate;

	/// <summary>
	/// Rate at which pawns are sterilized for siring
	/// </summary>
	public static float sireSterilizationRate;

	/// <summary>
	/// Rate at which sterilizations are reversible vs permanent
	/// </summary>
	public static float reversibleSterilizationRate;

	/// <summary>
	/// Rate at which pawns have prosthetic carry organs instead of natural
	/// </summary>
	public static float prostheticCarryRate;

	/// <summary>
	/// Rate at which pawns have prosthetic sire organs instead of natural
	/// </summary>
	public static float prostheticSireRate;

	/// <summary>
	/// Rate at which prosthetics are bionic instead of basic
	/// </summary>
	public static float bionicUpgradeRate;

	#endregion

	#region Hediff Definitions

	/// <summary>
	/// Hediff definition for cisgender identity
	/// </summary>
	public static readonly HediffDef cisDef = HediffDef.Named("Cisgender");

	/// <summary>
	/// Hediff definition for transgender identity
	/// </summary>
	public static readonly HediffDef transDef = HediffDef.Named("Transgender");

	/// <summary>
	/// Hediff definition for pregnancy carrying ability
	/// </summary>
	public static readonly HediffDef canCarryDef = HediffDef.Named("PregnancyCarry");

	/// <summary>
	/// Hediff definition for pregnancy siring ability
	/// </summary>
	public static readonly HediffDef canSireDef = HediffDef.Named("PregnancySire");

	#endregion

	#region Public Query Methods

	/// <summary>
	/// Determines if a pawn can carry pregnancies
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>True if the pawn can carry pregnancies</returns>
	public static bool CanCarry(Pawn pawn)
	{
		return CanCarryReport(pawn).Accepted;
	}

	/// <summary>
	/// Determines if a pawn can carry pregnancies with detailed reason
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>AcceptanceReport with reason if unable to carry</returns>
	public static AcceptanceReport CanCarryReport(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null)
		{
			SimpleTransDebug.Log("CanCarryReport called with null pawn or missing health data", 1);
			return "CannotNoAbility".Translate();
		}

		// Check if pawn has carry capability
		if (!pawn.health.hediffSet.HasHediff(canCarryDef, false))
		{
			return "CannotNoAbility".Translate();
		}

		// Check for sterilization that blocks carrying
		if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Sterilized) != null)
		{
			return "CannotSterile".Translate();
		}

		var sterilizedCarryDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedCarry");
		if (sterilizedCarryDef != null && pawn.health.hediffSet.HasHediff(sterilizedCarryDef, false))
		{
			return "CannotSterile".Translate();
		}

		var reversibleSterilizedCarryDef = DefDatabase<HediffDef>.GetNamedSilentFail("ReversibleSterilizedCarry");
		if (reversibleSterilizedCarryDef != null && pawn.health.hediffSet.HasHediff(reversibleSterilizedCarryDef, false))
		{
			return "CannotSterile".Translate();
		}

		return true;
	}

	/// <summary>
	/// Determines if a pawn can sire offspring
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>True if the pawn can sire offspring</returns>
	public static bool CanSire(Pawn pawn)
	{
		return CanSireReport(pawn).Accepted;
	}

	/// <summary>
	/// Determines if a pawn can sire offspring with detailed reason
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>AcceptanceReport with reason if unable to sire</returns>
	public static AcceptanceReport CanSireReport(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null)
		{
			SimpleTransDebug.Log("CanSireReport called with null pawn or missing health data", 1);
			return "CannotNoAbility".Translate();
		}

		// Check if pawn has sire capability
		if (!pawn.health.hediffSet.HasHediff(canSireDef, false))
		{
			return "CannotNoAbility".Translate();
		}

		// Check for sterilization that blocks siring
		if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Sterilized) != null)
		{
			return "CannotSterile".Translate();
		}

		var sterilizedSireDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedSire");
		if (sterilizedSireDef != null && pawn.health.hediffSet.HasHediff(sterilizedSireDef, false))
		{
			return "CannotSterile".Translate();
		}

		var reversibleSterilizedSireDef = DefDatabase<HediffDef>.GetNamedSilentFail("ReversibleSterilizedSire");
		if (reversibleSterilizedSireDef != null && pawn.health.hediffSet.HasHediff(reversibleSterilizedSireDef, false))
		{
			return "CannotSterile".Translate();
		}

		return true;
	}

	/// <summary>
	/// Determines if a pawn is cisgender (has the Cisgender hediff)
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>True if the pawn is cisgender</returns>
	public static bool IsCisgender(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null)
		{
			return false;
		}
		
		return pawn.health.hediffSet.HasHediff(cisDef, false);
	}

	#endregion

	#region Unified Generation Logic

	/// <summary>
	/// Determines gender identity and reproductive capabilities for a pawn using consistent logic
	/// Used for both new pawn generation and updating existing pawns
	/// </summary>
	/// <param name="pawn">The pawn to evaluate</param>
	/// <returns>Tuple of (isTransgender, canCarry, canSire)</returns>
	public static (bool isTransgender, bool canCarry, bool canSire) DetermineGenderAndCapabilities(Pawn pawn)
	{
		if (pawn == null)
		{
			Log.Error("[Simple Trans] DetermineGenderAndCapabilities called with null pawn");
			return (false, false, false);
		}

		// Settings are loaded once and cached, no need to reload per pawn

		bool isTransgender;
		bool canCarry = false;
		bool canSire = false;


		// Non-binary pawns are always considered transgender
		if (pawn.gender != Gender.Male && pawn.gender != Gender.Female)
		{
			isTransgender = true;

			// Enhanced non-binary logic with both/neither/single options
			float specialCaseRoll = Rand.Range(0f, 1f);
			if (specialCaseRoll < enbyBothRate)
			{
				// Both abilities
				canCarry = true;
				canSire = true;
			}
			else if (specialCaseRoll < enbyBothRate + enbyNeitherRate)
			{
				// Neither ability
				canCarry = false;
				canSire = false;
			}
			else
			{
				// Single ability - use fresh roll and enbyCarryRate to determine which one
				float abilityRoll = Rand.Range(0f, 1f);
				if (abilityRoll < enbyCarryRate)
				{
					canCarry = true;
					canSire = false;
				}
				else
				{
					canCarry = false;
					canSire = true;
				}
			}
		}
		else
		{
			// Binary gender logic
			isTransgender = Rand.Range(0f, 1f) > cisRate;

			if (isTransgender)
			{
				// Transgender binary logic
				float specialCaseRoll = Rand.Range(0f, 1f);
				if (specialCaseRoll < transBothRate)
				{
					// Both abilities
					canCarry = true;
					canSire = true;
				}
				else if (specialCaseRoll < transBothRate + transNeitherRate)
				{
					// Neither ability
					canCarry = false;
					canSire = false;
				}
				else
				{
					// Standard trans logic - use a fresh roll for ability determination
					float abilityRoll = Rand.Range(0f, 1f);
					if (pawn.gender == Gender.Male)
					{
						canCarry = abilityRoll < transManCarryPercent;
						canSire = !canCarry;
					}
					else if (pawn.gender == Gender.Female)
					{
						canSire = abilityRoll < transWomanSireRate;
						canCarry = !canSire;
					}
				}
			}
			else
			{
				// Cisgender logic
				float specialCaseRoll = Rand.Range(0f, 1f);
				if (specialCaseRoll < cisBothRate)
				{
					// Both abilities (rare)
					canCarry = true;
					canSire = true;
				}
				else if (pawn.gender == Gender.Male)
				{
					// Fresh roll for cis man ability determination
					float abilityRoll = Rand.Range(0f, 1f);
					canCarry = abilityRoll < cisManCarryRate;
					canSire = !canCarry;
				}
				else if (pawn.gender == Gender.Female)
				{
					// Fresh roll for cis woman ability determination
					float abilityRoll = Rand.Range(0f, 1f);
					canSire = abilityRoll < cisWomanSireRate;
					canCarry = !canSire;
				}
			}
		}

		string genderString = pawn.gender switch
		{
			Gender.Male => "male",
			Gender.Female => "female",
			_ when (int)pawn.gender == 3 => "enby",
			_ => $"unknown({(int)pawn.gender})"
		};
		
		SimpleTransDebug.Log($"Generated pawn: {pawn.Name?.ToStringShort ?? "unknown"} - Gender: {genderString}, Trans: {isTransgender}, Carry: {canCarry}, Sire: {canSire}", 1);

		return (isTransgender, canCarry, canSire);
	}

	/// <summary>
	/// Applies reproductive capabilities to a pawn with prosthetic and sterilization generation
	/// Universal rates apply to all pawns regardless of gender identity
	/// </summary>
	/// <param name="pawn">The pawn to modify</param>
	/// <param name="canCarry">Whether the pawn should have carrying capability</param>
	/// <param name="canSire">Whether the pawn should have siring capability</param>
	public static void ApplyReproductiveCapabilities(Pawn pawn, bool canCarry, bool canSire)
	{
		if (pawn?.health?.hediffSet == null)
		{
			Log.Error("[Simple Trans] ApplyReproductiveCapabilities called with null pawn or missing health data");
			return;
		}

		try
		{
			// Apply carry capability
			if (canCarry)
			{
				SetCarry(pawn, false);

				// Roll for prosthetic replacement (only if prosthetics are enabled)
				if (enableProsthetics && Rand.Range(0f, 1f) < prostheticCarryRate)
				{
					// Apply prosthetic modifier
					if (Rand.Range(0f, 1f) < bionicUpgradeRate)
					{
						var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicProstheticCarry");
						if (hediffDef != null)
						{
							pawn.health.AddHediff(hediffDef);
						}
						else
						{
							Log.Warning("[Simple Trans] BionicProstheticCarry hediff not found - prosthetics may be disabled");
						}
					}
					else
					{
						var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BasicProstheticCarry");
						if (hediffDef != null)
						{
							pawn.health.AddHediff(hediffDef);
						}
						else
						{
							Log.Warning("[Simple Trans] BasicProstheticCarry hediff not found - prosthetics may be disabled");
						}
					}
				}
				else
				{
					// Natural organs - roll for sterilization
					if (Rand.Range(0f, 1f) < carrySterilizationRate)
					{
						if (Rand.Range(0f, 1f) < reversibleSterilizationRate)
						{
							var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("ReversibleSterilizedCarry");
							if (hediffDef != null)
							{
								pawn.health.AddHediff(hediffDef);
							}
						}
						else
						{
							var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedCarry");
							if (hediffDef != null)
							{
								pawn.health.AddHediff(hediffDef);
							}
						}
					}
				}
			}

			// Apply sire capability (same pattern)
			if (canSire)
			{
				SetSire(pawn, false);

				// Roll for prosthetic replacement (only if prosthetics are enabled)
				if (enableProsthetics && Rand.Range(0f, 1f) < prostheticSireRate)
				{
					// Apply prosthetic modifier
					if (Rand.Range(0f, 1f) < bionicUpgradeRate)
					{
						var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicProstheticSire");
						if (hediffDef != null)
						{
							pawn.health.AddHediff(hediffDef);
						}
						else
						{
							Log.Warning("[Simple Trans] BionicProstheticSire hediff not found - prosthetics may be disabled");
						}
					}
					else
					{
						var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("BasicProstheticSire");
						if (hediffDef != null)
						{
							pawn.health.AddHediff(hediffDef);
						}
						else
						{
							Log.Warning("[Simple Trans] BasicProstheticSire hediff not found - prosthetics may be disabled");
						}
					}
				}
				else
				{
					// Natural organs - roll for sterilization
					if (Rand.Range(0f, 1f) < sireSterilizationRate)
					{
						if (Rand.Range(0f, 1f) < reversibleSterilizationRate)
						{
							var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("ReversibleSterilizedSire");
							if (hediffDef != null)
							{
								pawn.health.AddHediff(hediffDef);
							}
						}
						else
						{
							var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedSire");
							if (hediffDef != null)
							{
								pawn.health.AddHediff(hediffDef);
							}
						}
					}
				}
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error applying reproductive capabilities for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}

	#endregion

	#region Core Gender Logic

	/// <summary>
	/// Main entry point for validating or setting gender identity and reproductive capabilities
	/// Uses unified logic to ensure consistency between new pawns and existing pawns
	/// </summary>
	/// <param name="pawn">The pawn to process</param>
	public static void ValidateOrSetGender(Pawn pawn)
	{
		try
		{
			// Only process humanlike pawns - animals don't have gender identity
			if (pawn?.RaceProps?.Humanlike != true)
			{
				return;
			}

			// Ensure genes system exists for this pawn
			if (pawn.genes?.GenesListForReading == null)
			{
				return;
			}

			// Check if pawn is pregnant - if so, skip capability clearing to preserve pregnancy
			bool isPregnant = pawn.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman);

			// Clear any existing gender and capability hediffs to start fresh
			// BUT preserve capabilities if pregnant to avoid terminating pregnancy
			ClearGender(pawn, clearIdentity: true, clearCapabilities: !isPregnant);

			// If pregnant, ensure they have carry ability (they must have it to be pregnant)
			if (isPregnant && !CanCarry(pawn))
			{
				SetCarry(pawn, false, clearSterilization: true);
				SimpleTransDebug.Log($"Added carry ability to pregnant pawn {pawn.Name?.ToStringShort}", 2);
			}

			// Use unified determination logic
			var (isTransgender, canCarry, canSire) = DetermineGenderAndCapabilities(pawn);

			// Apply identity hediff
			if (isTransgender)
			{
				SetTrans(pawn);
			}
			else
			{
				SetCis(pawn);
			}

			// Apply reproductive capabilities with prosthetics and sterilization
			// BUT skip if pregnant to avoid interfering with ongoing pregnancy
			if (!isPregnant)
			{
				ApplyReproductiveCapabilities(pawn, canCarry, canSire);
			}

			// Handle backwards compatibility: convert vanilla Sterilized to capability-specific sterilization
			ConvertVanillaSterilizedHediff(pawn, isTransgender);

			// Process genes for additional gender/reproductive overrides
			// This handles VEF (Vanilla Expanded Framework) gene extensions that can force specific genders
			// Only processes if genesAreAgab setting is enabled (genes represent assigned gender at birth)
			foreach (Gene gene in pawn.genes.GenesListForReading)
			{
				if (gene?.def != null)
				{
					ValidateOrSetGenderWithGenes(pawn, gene);
				}
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error in ValidateOrSetGender for pawn {pawn?.Name?.ToStringShort ?? "null"}: {ex}");
		}
	}

	/// <summary>
	/// Whether organ transplant system is enabled
	/// </summary>
	public static bool enableOrganTransplants;

	/// <summary>
	/// Whether prosthetic system is enabled
	/// </summary>
	public static bool enableProsthetics;

	/// <summary>
	/// Whether ideology ritual system is enabled
	/// </summary>
	public static bool enableIdeologyRitual;

	/// <summary>
	/// Whether ideology biosculpter cycle is enabled
	/// </summary>
	public static bool enableIdeologyBiosculpter;


	/// <summary>
	/// Checks if a pawn is non-binary
	/// Default implementation returns false, but NBG mod patches this to return EnbyUtility.IsEnby(pawn)
	/// </summary>
	/// <param name="pawn">The pawn to check</param>
	/// <returns>True if the pawn is non-binary, false otherwise</returns>
	public static bool IsEnby(Pawn pawn)
	{
		return false; // Default implementation - our NBGPatches patches this
	}


	/// <summary>
	/// Loads mod settings from the settings manager
	/// </summary>
	public static void LoadSettings()
	{
		try
		{
			// Load settings with null safety
			string cisPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "cisPercent");
			string transManCarryPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "transManCarryPercent");
			string transWomanSirePercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "transWomanSirePercent");
			string enbyCarryPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "enbyCarryPercent");
			string genesAreAgabSetting = SettingsManager.GetSetting("runaway.simpletrans", "genesAreAgab");
			string enableOrganTransplantsSetting = SettingsManager.GetSetting("runaway.simpletrans", "enableOrganTransplants");
			string enableProstheticsSetting = SettingsManager.GetSetting("runaway.simpletrans", "enableProsthetics");
			string enableIdeologyRitualSetting = SettingsManager.GetSetting("runaway.simpletrans", "enableIdeologyRitual");
			string enableIdeologyBiosculpterSetting = SettingsManager.GetSetting("runaway.simpletrans", "enableIdeologyBiosculpter");
			string transBothPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "transBothPercent");
			string transNeitherPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "transNeitherPercent");
			string cisManCarryPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "cisManCarryPercent");
			string cisWomanSirePercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "cisWomanSirePercent");
			string cisBothPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "cisBothPercent");
			string enbyBothPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "enbyBothPercent");
			string enbyNeitherPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "enbyNeitherPercent");
			string carrySterilizationPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "carrySterilizationPercent");
			string sireSterilizationPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "sireSterilizationPercent");
			string reversibleSterilizationPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "reversibleSterilizationPercent");
			string prostheticCarryPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "prostheticCarryPercent");
			string prostheticSirePercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "prostheticSirePercent");
			string bionicUpgradePercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "bionicUpgradePercent");

			// Parse settings with error handling and defaults
			cisRate = TryParseFloat(cisPercentSetting, SimpleTransConstants.DefaultCisRate * SimpleTransConstants.PercentageToDecimal) / SimpleTransConstants.PercentageToDecimal;
			transManCarryPercent = TryParseFloat(transManCarryPercentSetting, SimpleTransConstants.DefaultTransManCarryPercent * SimpleTransConstants.PercentageToDecimal) / SimpleTransConstants.PercentageToDecimal;
			SimpleTransDebug.Log($"Settings loaded - cisRate: {cisRate:F3}, transManCarryPercent: {transManCarryPercent:F3}", 2);
			transWomanSireRate = TryParseFloat(transWomanSirePercentSetting, SimpleTransConstants.DefaultTransWomanSireRate * SimpleTransConstants.PercentageToDecimal) / SimpleTransConstants.PercentageToDecimal;
			genesAreAgab = TryParseBool(genesAreAgabSetting, true);
			enableOrganTransplants = TryParseBool(enableOrganTransplantsSetting, true);
			enableProsthetics = TryParseBool(enableProstheticsSetting, true);
			enableIdeologyRitual = TryParseBool(enableIdeologyRitualSetting, true);
			enableIdeologyBiosculpter = TryParseBool(enableIdeologyBiosculpterSetting, true);
			transBothRate = TryParseFloat(transBothPercentSetting, 5f) / SimpleTransConstants.PercentageToDecimal;
			transNeitherRate = TryParseFloat(transNeitherPercentSetting, 5f) / SimpleTransConstants.PercentageToDecimal;
			cisManCarryRate = TryParseFloat(cisManCarryPercentSetting, 1f) / SimpleTransConstants.PercentageToDecimal;
			cisWomanSireRate = TryParseFloat(cisWomanSirePercentSetting, 1f) / SimpleTransConstants.PercentageToDecimal;
			cisBothRate = TryParseFloat(cisBothPercentSetting, 0f) / SimpleTransConstants.PercentageToDecimal;
			enbyCarryRate = TryParseFloat(enbyCarryPercentSetting, SimpleTransConstants.DefaultEnbyCarryRate * SimpleTransConstants.PercentageToDecimal) / SimpleTransConstants.PercentageToDecimal;
			enbyBothRate = TryParseFloat(enbyBothPercentSetting, 10f) / SimpleTransConstants.PercentageToDecimal;
			enbyNeitherRate = TryParseFloat(enbyNeitherPercentSetting, 15f) / SimpleTransConstants.PercentageToDecimal;
			carrySterilizationRate = TryParseFloat(carrySterilizationPercentSetting, 3f) / SimpleTransConstants.PercentageToDecimal;
			sireSterilizationRate = TryParseFloat(sireSterilizationPercentSetting, 3f) / SimpleTransConstants.PercentageToDecimal;
			reversibleSterilizationRate = TryParseFloat(reversibleSterilizationPercentSetting, 60f) / SimpleTransConstants.PercentageToDecimal;
			prostheticCarryRate = TryParseFloat(prostheticCarryPercentSetting, 2f) / SimpleTransConstants.PercentageToDecimal;
			prostheticSireRate = TryParseFloat(prostheticSirePercentSetting, 2f) / SimpleTransConstants.PercentageToDecimal;
			bionicUpgradeRate = TryParseFloat(bionicUpgradePercentSetting, 20f) / SimpleTransConstants.PercentageToDecimal;
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error loading settings, using defaults: {ex}");
			// Set all defaults
			cisRate = SimpleTransConstants.DefaultCisRate;
			transManCarryPercent = SimpleTransConstants.DefaultTransManCarryPercent;
			transWomanSireRate = SimpleTransConstants.DefaultTransWomanSireRate;
			genesAreAgab = true;
			enableOrganTransplants = true;
			enableProsthetics = true;
			enableIdeologyRitual = true;
			enableIdeologyBiosculpter = true;
			transBothRate = 0.05f;
			transNeitherRate = 0.05f;
			cisManCarryRate = 0.01f;
			cisWomanSireRate = 0.01f;
			cisBothRate = 0f;
			enbyCarryRate = SimpleTransConstants.DefaultEnbyCarryRate;
			enbyBothRate = 0.10f;
			enbyNeitherRate = 0.15f;
			carrySterilizationRate = 0.03f;
			sireSterilizationRate = 0.03f;
			reversibleSterilizationRate = 0.60f;
			prostheticCarryRate = 0.02f;
			prostheticSireRate = 0.02f;
			bionicUpgradeRate = 0.20f;
		}
	}

	#endregion

	#region Gender Assignment Methods

	/// <summary>
	/// Sets a pawn as transgender by adding the transgender hediff and removing cisgender hediff
	/// </summary>
	/// <param name="pawn">The pawn to modify</param>
	public static void SetTrans(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null)
		{
			Log.Error("[Simple Trans] SetTrans called with null pawn or missing health data");
			return;
		}

		try
		{
			if (!pawn.health.hediffSet.HasHediff(transDef, false))
			{
				pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(cisDef));
				pawn.health.GetOrAddHediff(transDef);
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error setting transgender hediff for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}

	/// <summary>
	/// Sets a pawn as cisgender by adding the cisgender hediff and removing transgender hediff
	/// </summary>
	/// <param name="pawn">The pawn to modify</param>
	public static void SetCis(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null)
		{
			Log.Error("[Simple Trans] SetCis called with null pawn or missing health data");
			return;
		}

		try
		{
			// Warn if setting non-binary pawn to cisgender
			if (IsEnby(pawn))
			{
				Log.Warning("[Simple Trans] Setting non-binary pawn to cisgender - this may cause unexpected behavior.");
			}

			if (!pawn.health.hediffSet.HasHediff(cisDef, false))
			{
				pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(transDef));
				pawn.health.GetOrAddHediff(cisDef);
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error setting cisgender hediff for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}

	#endregion

	#region Reproductive Ability Methods

	/// <summary>
	/// Grants a pawn the ability to carry pregnancies
	/// </summary>
	/// <param name="pawn">The pawn to modify</param>
	/// <param name="removeSire">Whether to remove siring ability (for exclusive reproductive roles)</param>
	public static void SetCarry(Pawn pawn, bool removeSire = false, bool clearSterilization = false)
	{
		if (pawn?.health?.hediffSet == null)
		{
			Log.Error("[Simple Trans] SetCarry called with null pawn or missing health data");
			return;
		}

		try
		{
			if (!pawn.health.hediffSet.HasHediff(canCarryDef, false))
			{
				pawn.health.GetOrAddHediff(canCarryDef);
				if ((removeSire || pawn.gender == Gender.Female) && pawn.health.hediffSet.HasHediff(canSireDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(canSireDef));
				}
			}

			// Optionally clear carry-related sterilization
			if (clearSterilization)
			{
				var sterilizedCarryDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedCarry");
				if (sterilizedCarryDef != null && pawn.health.hediffSet.HasHediff(sterilizedCarryDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(sterilizedCarryDef));
					SimpleTransDebug.Log($"Cleared carry sterilization for {pawn.Name?.ToStringShort}", 2);
				}

				var reversibleSterilizedCarryDef = DefDatabase<HediffDef>.GetNamedSilentFail("ReversibleSterilizedCarry");
				if (reversibleSterilizedCarryDef != null && pawn.health.hediffSet.HasHediff(reversibleSterilizedCarryDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(reversibleSterilizedCarryDef));
					SimpleTransDebug.Log($"Cleared reversible carry sterilization for {pawn.Name?.ToStringShort}", 2);
				}

				// Also clear vanilla sterilization since it blocks all reproduction
				if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Sterilized) != null)
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(HediffDefOf.Sterilized));
					SimpleTransDebug.Log($"Cleared vanilla sterilization for {pawn.Name?.ToStringShort}", 2);
				}
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error setting carry hediff for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}

	/// <summary>
	/// Grants a pawn the ability to sire offspring
	/// </summary>
	/// <param name="pawn">The pawn to modify</param>
	/// <param name="removeCarry">Whether to remove carrying ability (for exclusive reproductive roles)</param>
	public static void SetSire(Pawn pawn, bool removeCarry = false, bool clearSterilization = false)
	{
		if (pawn?.health?.hediffSet == null)
		{
			Log.Error("[Simple Trans] SetSire called with null pawn or missing health data");
			return;
		}

		try
		{
			if (!pawn.health.hediffSet.HasHediff(canSireDef, false))
			{
				pawn.health.GetOrAddHediff(canSireDef);
				if ((removeCarry || pawn.gender == Gender.Male) && pawn.health.hediffSet.HasHediff(canCarryDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(canCarryDef));
				}
			}

			// Optionally clear sire-related sterilization
			if (clearSterilization)
			{
				var sterilizedSireDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedSire");
				if (sterilizedSireDef != null && pawn.health.hediffSet.HasHediff(sterilizedSireDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(sterilizedSireDef));
					SimpleTransDebug.Log($"Cleared sire sterilization for {pawn.Name?.ToStringShort}", 2);
				}

				var reversibleSterilizedSireDef = DefDatabase<HediffDef>.GetNamedSilentFail("ReversibleSterilizedSire");
				if (reversibleSterilizedSireDef != null && pawn.health.hediffSet.HasHediff(reversibleSterilizedSireDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(reversibleSterilizedSireDef));
					SimpleTransDebug.Log($"Cleared reversible sire sterilization for {pawn.Name?.ToStringShort}", 2);
				}

				// Also clear vanilla sterilization since it blocks all reproduction
				if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Sterilized) != null)
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(HediffDefOf.Sterilized));
					SimpleTransDebug.Log($"Cleared vanilla sterilization for {pawn.Name?.ToStringShort}", 2);
				}
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error setting sire hediff for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}

	#endregion

	#region Gene Integration

	/// <summary>
	/// Validates or sets gender based on gene extensions (VEF integration)
	/// </summary>
	/// <param name="pawn">The pawn to process</param>
	/// <param name="gene">The gene to evaluate</param>
	public static void ValidateOrSetGenderWithGenes(Pawn pawn, Gene gene)
	{
		if (pawn == null)
		{
			Log.Error("[Simple Trans] ValidateOrSetGenderWithGenes called with null pawn");
			return;
		}

		if (gene?.def == null)
		{
			SimpleTransDebug.Log("ValidateOrSetGenderWithGenes called with null gene or gene definition", 2);
			return;
		}

		try
		{
			// Check if this gene has VEF gender-forcing extensions
			GeneExtension modExtension = ((Def)gene.def).GetModExtension<GeneExtension>();
			if (modExtension == null || !genesAreAgab)
			{
				// Skip processing if no extension or genes don't represent AGAB (assigned gender at birth)
				return;
			}

			// Special non-binary handling
			if (IsEnby(pawn) && (modExtension.forceFemale || modExtension.forceMale))
			{
				SetTrans(pawn);

				if (Rand.Range(0f, 1f) < enbyBothRate)
				{
					SetCarry(pawn, false);
					SetSire(pawn, false);
				}
				else if (Rand.Range(0f, 1f) < enbyNeitherRate)
				{
					// Don't set carry or sire
				}
				else
				{
					if (modExtension.forceFemale)
					{
						SetCarry(pawn, true); // Force carry ability
					}
					else
					{
						SetSire(pawn, true); // Force sire ability
					}
				}

				return;
			}

			// Handle genes that force female reproductive capabilities
			if (modExtension.forceFemale)
			{
				// Gene forces female reproductive role, but gender identity can still vary
				// If transgender rate applies, pawn becomes trans male (M gender, F reproductive role)
				// Otherwise becomes cis female (F gender, F reproductive role)
				// Don't mess with non-binary people
				pawn.gender = (Rand.Range(0f, 1f) > cisRate) ? Gender.Male : Gender.Female;

				// Clear any existing reproductive hediffs first (but preserve identity as we're about to set it)
				ClearGender(pawn, clearIdentity: false, clearCapabilities: true);

				if (pawn.gender == Gender.Male)
				{
					// Trans man: masculine gender identity, can carry pregnancies
					SetTrans(pawn);

					if (Rand.Range(0f, 1f) < transBothRate)
					{
						// Rare case: trans male with both abilities
						SetCarry(pawn, false);
						SetSire(pawn, false);
					}
					else if (Rand.Range(0f, 1f) < transNeitherRate)
					{
						// Don't set carry or sire
						return;
					}
					else
					{
						// Standard: always grant carrying ability for forceFemale genes
						SetCarry(pawn, true);
					}
				}
				else if (pawn.gender == Gender.Female)
				{
					// Cis woman: feminine gender identity, can carry pregnancies
					SetCis(pawn);
					if (Rand.Range(0f, 1f) < cisBothRate)
					{
						// Rare case: cis female with both abilities
						SetCarry(pawn, false);
						SetSire(pawn, false);
					}
					else
					{
						SetCarry(pawn, true);
					}
				}
				else // Secret fourth thing?
				{
					SetCarry(pawn, true);
				}
			}

			// Handle genes that force male reproductive capabilities
			if (modExtension.forceMale)
			{
				// Gene forces male reproductive role, but gender identity can still vary
				// If above cis rate, assign female gender (trans female)
				pawn.gender = (Rand.Range(0f, 1f) > cisRate) ? Gender.Female : Gender.Male;

				// Clear any existing reproductive hediffs first (but preserve identity as we're about to set it)
				ClearGender(pawn, clearIdentity: false, clearCapabilities: true);

				if (pawn.gender == Gender.Female)
				{
					// Trans woman: feminine gender identity, can sire pregnancies
					SetTrans(pawn);
					if (Rand.Range(0f, 1f) < transBothRate)
					{
						// Rare case: trans woman with both abilities
						SetCarry(pawn, false);
						SetSire(pawn, false);
					}
					else if (Rand.Range(0f, 1f) < transNeitherRate)
					{
						// Don't set carry or sire
						return;
					}
					else
					{
						// Standard: always grant siring ability for forceMale genes
						SetSire(pawn, true);
					}
				}
				else if (pawn.gender == Gender.Male)
				{
					// Cis man: masculine gender identity, can sire pregnancies
					SetCis(pawn);
					if (Rand.Range(0f, 1f) < cisBothRate)
					{
						// Rare case: cis man with both abilities
						SetCarry(pawn, false);
						SetSire(pawn, false);
					}
					else
					{
						SetSire(pawn, true);
					}
				}
				else // Secret fourth thing?
				{
					SetSire(pawn, true);
				}
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error in ValidateOrSetGenderWithGenes for {pawn?.Name?.ToStringShort ?? "unknown"} with gene {gene?.def?.defName ?? "unknown"}: {ex}");
		}
	}

	#endregion

	#region Helper Methods

	/// <summary>
	/// Safely parses a float value with fallback to default
	/// </summary>
	/// <param name="value">The string value to parse</param>
	/// <param name="defaultValue">The default value if parsing fails</param>
	/// <returns>The parsed float or default value</returns>
	private static float TryParseFloat(string value, float defaultValue)
	{
		if (string.IsNullOrEmpty(value))
		{
			return defaultValue;
		}

		if (float.TryParse(value, out float result))
		{
			return result;
		}

		Log.Warning($"[Simple Trans] Failed to parse float value '{value}', using default {defaultValue}");
		return defaultValue;
	}

	/// <summary>
	/// Safely parses a boolean value with fallback to default
	/// </summary>
	/// <param name="value">The string value to parse</param>
	/// <param name="defaultValue">The default value if parsing fails</param>
	/// <returns>The parsed boolean or default value</returns>
	private static bool TryParseBool(string value, bool defaultValue)
	{
		if (string.IsNullOrEmpty(value))
		{
			return defaultValue;
		}

		if (bool.TryParse(value, out bool result))
		{
			return result;
		}

		Log.Warning($"[Simple Trans] Failed to parse boolean value '{value}', using default {defaultValue}");
		return defaultValue;
	}

	#endregion

	#region Utility Methods

	/// <summary>
	/// Removes gender-related and/or reproductive hediffs from a pawn
	/// </summary>
	/// <param name="pawn">The pawn to clear</param>
	/// <param name="clearIdentity">Whether to clear identity hediffs (cis/trans)</param>
	/// <param name="clearCapabilities">Whether to clear reproductive capability hediffs (carry/sire)</param>
	public static void ClearGender(Pawn pawn, bool clearIdentity = true, bool clearCapabilities = true)
	{
		if (pawn?.health?.hediffSet == null)
		{
			Log.Error("[Simple Trans] ClearGender called with null pawn or missing health data");
			return;
		}

		try
		{
			// Remove identity hediffs if requested
			if (clearIdentity)
			{
				if (pawn.health.hediffSet.HasHediff(cisDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(cisDef));
				}
				if (pawn.health.hediffSet.HasHediff(transDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(transDef));
				}
			}

			// Remove reproductive capability hediffs if requested
			if (clearCapabilities)
			{
				if (pawn.health.hediffSet.HasHediff(canCarryDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(canCarryDef));
				}
				if (pawn.health.hediffSet.HasHediff(canSireDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(canSireDef));
				}
			}

			// Also remove prosthetic and sterilization hediffs if clearing capabilities
			if (clearCapabilities)
			{
				var hediffsToRemove = pawn.health.hediffSet.hediffs.Where(h =>
					h.def.defName == "BasicProstheticCarry" ||
					h.def.defName == "BasicProstheticSire" ||
					h.def.defName == "BionicProstheticCarry" ||
					h.def.defName == "BionicProstheticSire" ||
					h.def.defName == "SterilizedCarry" ||
					h.def.defName == "SterilizedSire" ||
					h.def.defName == "ReversibleSterilizedCarry" ||
					h.def.defName == "ReversibleSterilizedSire" ||
					h.def.defName == "Sterilized").ToList();

				foreach (var hediff in hediffsToRemove)
				{
					pawn.health.RemoveHediff(hediff);
				}
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error clearing gender hediffs for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}

	/// <summary>
	/// Core conversion logic for vanilla Sterilized hediff to capability-specific sterilization
	/// </summary>
	/// <param name="pawn">The pawn to convert</param>
	/// <param name="forceCarryingSterilization">If true, force carrying sterilization regardless of logic</param>
	/// <param name="forceSiringSterilization">If true, force siring sterilization regardless of logic</param>
	/// <param name="useOppositeCapabilityLogic">If true, use opposite capability logic (for adding abilities)</param>
	/// <param name="targetAbility">The ability being added (only used with opposite capability logic)</param>
	/// <returns>True if conversion was performed, false if no vanilla sterilized hediff was found</returns>
	public static bool ConvertVanillaSterilizedHediffCore(Pawn pawn, bool forceCarryingSterilization = false, bool forceSiringSterilization = false, bool useOppositeCapabilityLogic = false, AbilityType? targetAbility = null)
	{
		if (pawn?.health?.hediffSet == null) return false;

		try
		{
			// Check if pawn has vanilla Sterilized hediff
			var vanillaSterilized = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Sterilized);
			if (vanillaSterilized == null) return false;

			// Remove vanilla sterilized hediff
			pawn.health.RemoveHediff(vanillaSterilized);

			// Determine which capability should be sterilized
			bool shouldSterilizeCarry = forceCarryingSterilization;
			bool shouldSterilizeSire = forceSiringSterilization;

			if (!forceCarryingSterilization && !forceSiringSterilization)
			{
				if (useOppositeCapabilityLogic && targetAbility.HasValue)
				{
					// Opposite capability logic: if adding carry ability, sterilize siring (and vice versa)
					if (targetAbility == AbilityType.Carry)
					{
						shouldSterilizeSire = true;
					}
					else if (targetAbility == AbilityType.Sire)
					{
						shouldSterilizeCarry = true;
					}
					else
					{
						// Fall back to AGAB logic for unknown ability types
						useOppositeCapabilityLogic = false;
					}
				}

				if (!useOppositeCapabilityLogic)
				{
					// AGAB-based logic
					bool isTransgender = pawn.health.hediffSet.HasHediff(transDef);

					if (isTransgender)
					{
						// For trans pawns, sterilization affects their birth-assigned capability
						if (pawn.gender == Gender.Male)
						{
							// Trans male (AFAB) - sterilization would have been tubal ligation
							shouldSterilizeCarry = true;
						}
						else if (pawn.gender == Gender.Female)
						{
							// Trans female (AMAB) - sterilization would have been vasectomy
							shouldSterilizeSire = true;
						}
					}
					else
					{
						// For cis pawns, sterilization affects their current gender's capability
						if (pawn.gender == Gender.Male)
						{
							// Cis male - sterilization would have been vasectomy
							shouldSterilizeSire = true;
						}
						else if (pawn.gender == Gender.Female)
						{
							// Cis female - sterilization would have been tubal ligation
							shouldSterilizeCarry = true;
						}
					}
				}
			}

			// Apply capability-specific sterilization
			if (shouldSterilizeCarry)
			{
				var sterilizedCarryDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedCarry");
				if (sterilizedCarryDef != null)
				{
					var hediff = HediffMaker.MakeHediff(sterilizedCarryDef, pawn);
					pawn.health.AddHediff(hediff);
					SimpleTransDebug.Log($"Converted vanilla Sterilized to SterilizedCarry for {pawn.Name}", 2);
				}
			}

			if (shouldSterilizeSire)
			{
				var sterilizedSireDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedSire");
				if (sterilizedSireDef != null)
				{
					var hediff = HediffMaker.MakeHediff(sterilizedSireDef, pawn);
					pawn.health.AddHediff(hediff);
					SimpleTransDebug.Log($"Converted vanilla Sterilized to SterilizedSire for {pawn.Name}", 2);
				}
			}

			return true;
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error converting vanilla sterilized hediff for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
			return false;
		}
	}

	/// <summary>
	/// Ability types for sterilization conversion
	/// </summary>
	public enum AbilityType
	{
		Carry,
		Sire
	}

	/// <summary>
	/// Converts vanilla Sterilized hediffs to capability-specific sterilization for backwards compatibility
	/// Uses AGAB-based logic for determining which capability to sterilize
	/// </summary>
	/// <param name="pawn">The pawn to check and convert</param>
	/// <param name="isTransgender">Whether the pawn is transgender</param>
	private static void ConvertVanillaSterilizedHediff(Pawn pawn, bool isTransgender)
	{
		// Use the core conversion function with AGAB-based logic
		ConvertVanillaSterilizedHediffCore(pawn, useOppositeCapabilityLogic: false);
	}

	/// <summary>
	/// Unified pregnancy logic that handles capability-based role assignment and chance calculation
	/// </summary>
	/// <param name="pawn1">First pawn in the interaction</param>
	/// <param name="pawn2">Second pawn in the interaction (can be null for single-pawn scenarios)</param>
	/// <param name="baseChance">Base pregnancy chance (e.g., 0.05f for lovin')</param>
	/// <param name="findRandomFather">If true and no valid sirer found, attempts to find father from relations</param>
	/// <param name="progressOverride">Specific pregnancy progress (null for immediate pregnancy)</param>
	/// <param name="showIncompatibilityMessage">If true, shows user message when genes are incompatible</param>
	/// <returns>True if pregnancy was successfully created</returns>
	public static bool TryCreatePregnancy(Pawn pawn1, Pawn pawn2 = null, float baseChance = 1.0f, bool findRandomFather = false, float? progressOverride = null, bool showIncompatibilityMessage = false)
	{
		try
		{
			if (pawn1?.health == null)
			{
				Log.Error("[Simple Trans] TryCreatePregnancy called with null pawn1 or missing health data");
				return false;
			}

			// Determine reproductive roles based on capabilities
			Pawn sirer = null;
			Pawn carrier = null;

			if (pawn2 != null)
			{
				// Two-pawn scenario: determine roles from capabilities
				sirer = CanSire(pawn1) ? pawn1 : (CanSire(pawn2) ? pawn2 : null);
				carrier = CanCarry(pawn1) ? pawn1 : (CanCarry(pawn2) ? pawn2 : null);
			}
			else
			{
				// Single-pawn scenario: pawn1 must be the carrier
				if (CanCarry(pawn1))
				{
					carrier = pawn1;
					// sirer will be found randomly if findRandomFather is true
				}
			}

			// Validate we have a viable carrier
			if (carrier == null)
			{
				SimpleTransDebug.Log($"No viable carrier found for pregnancy", 2);
				return false;
			}

			// Find random father if requested and no sirer found
			if (sirer == null && findRandomFather)
			{
				if (carrier.relations?.DirectRelations != null &&
					!Rand.Chance(SimpleTransConstants.RandomFatherlessChance) &&
					GenCollection.TryRandomElementByWeight<DirectPawnRelation>(
						carrier.relations.DirectRelations.Where((DirectPawnRelation r) => PregnancyUtility.BeingFatherWeightPerRelation.ContainsKey(r.def)),
						(Func<DirectPawnRelation, float>)((DirectPawnRelation r) => PregnancyUtility.BeingFatherWeightPerRelation[r.def]),
						out DirectPawnRelation fatherRelation))
				{
					sirer = fatherRelation.otherPawn;
				}
			}

			// For two-pawn scenarios, check if we have viable reproductive pair
			if (pawn2 != null && sirer == null)
			{
				SimpleTransDebug.Log($"No viable sirer found for two-pawn pregnancy", 2);
				return false;
			}

			// Calculate pregnancy chance (skip for single-pawn background pregnancies)
			if (pawn2 != null)
			{
				float pregnancyChance = baseChance * PregnancyUtility.PregnancyChanceForPartners(carrier, sirer);

				if (!Rand.Chance(pregnancyChance))
				{
					SimpleTransDebug.Log($"Pregnancy chance failed: {pregnancyChance:F3}", 2);
					return false;
				}
			}

			// Create the actual pregnancy
			return CreatePregnancyHediff(carrier, sirer, progressOverride, findRandomFather, showIncompatibilityMessage);
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error in TryCreatePregnancy: {ex}");
			return false;
		}
	}

	/// <summary>
	/// Internal method that creates the actual pregnancy hediff
	/// </summary>
	private static bool CreatePregnancyHediff(Pawn carrier, Pawn sirer, float? progressOverride, bool isBackgroundPregnancy, bool showIncompatibilityMessage)
	{
		try
		{
			if (carrier?.health == null)
			{
				Log.Error("[Simple Trans] CreatePregnancyHediff called with null carrier or missing health data");
				return false;
			}

			// Check if already pregnant to avoid duplicates
			if (carrier.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman))
			{
				SimpleTransDebug.Log($"Carrier {carrier.Name?.ToStringShort} is already pregnant, skipping pregnancy creation", 1);
				return false;
			}

			// Create pregnancy hediff
			Hediff_Pregnant pregnancy = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, carrier, (BodyPartRecord)null);
			if (pregnancy == null)
			{
				Log.Error("[Simple Trans] Failed to create pregnancy hediff");
				return false;
			}

			// Set pregnancy progress
			if (progressOverride.HasValue)
			{
				pregnancy.Severity = progressOverride.Value;
			}
			else if (isBackgroundPregnancy)
			{
				// Background pregnancy - use random progress
				FloatRange generatedPawnPregnancyProgressRange = PregnancyUtility.GeneratedPawnPregnancyProgressRange;
				pregnancy.Severity = generatedPawnPregnancyProgressRange.RandomInRange;
			}
			// else: immediate pregnancy (severity defaults to 0)

			// Check gene compatibility
			bool genesCompatible;
			GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(sirer, carrier, out genesCompatible);

			if (genesCompatible)
			{
				pregnancy.SetParents(null, sirer, inheritedGeneSet);
				carrier.health.AddHediff(pregnancy);

				SimpleTransDebug.Log($"Pregnancy created: {carrier.Name?.ToStringShort ?? "unknown"} + {sirer?.Name?.ToStringShort ?? "none"}", 1);

				return true;
			}
			else
			{
				// Gene incompatibility
				if (showIncompatibilityMessage && sirer != null &&
					(PawnUtility.ShouldSendNotificationAbout(sirer) || PawnUtility.ShouldSendNotificationAbout(carrier)))
				{
					var message = "MessagePregnancyFailed".Translate(sirer.Named("FATHER"), carrier.Named("MOTHER")) +
								 ": " + "CombinedGenesExceedMetabolismLimits".Translate();
					Messages.Message(message, new LookTargets(sirer, carrier), MessageTypeDefOf.NegativeEvent);
				}

				SimpleTransDebug.Log($"Pregnancy failed - gene incompatibility: {carrier.Name?.ToStringShort ?? "unknown"} + {sirer?.Name?.ToStringShort ?? "none"}", 1);

				return false;
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error creating pregnancy hediff for {carrier?.Name?.ToStringShort ?? "unknown"}: {ex}");
			return false;
		}
	}

	#endregion
}
