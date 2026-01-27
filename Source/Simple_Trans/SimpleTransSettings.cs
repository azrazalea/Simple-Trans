using System;
using Verse;
using XmlExtensions;

namespace Simple_Trans;

/// <summary>
/// Configuration and settings management for Simple Trans mod
/// Handles loading and storing all mod configuration values
/// </summary>
public static class SimpleTransSettings
{
	#region Static Configuration Fields - Rates

	/// <summary>
	/// Current cisgender rate (percentage of pawns that are cisgender)
	/// </summary>
	public static float cisRate;

	/// <summary>
	/// Rate at which trans men can carry pregnancies
	/// </summary>
	public static float transManCarryRate;

	/// <summary>
	/// Rate at which trans women can sire offspring
	/// </summary>
	public static float transWomanSireRate;

	/// <summary>
	/// Rate at which non-binary pawns can carry pregnancies
	/// </summary>
	public static float enbyCarryRate;

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

	#region Static Configuration Fields - Boolean Settings

	/// <summary>
	/// Whether genes represent assigned gender at birth (AGAB)
	/// </summary>
	public static bool genesAreAgab;

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

	#endregion

	#region Settings Loading

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
			transManCarryRate = TryParseFloat(transManCarryPercentSetting, SimpleTransConstants.DefaultTransManCarryPercent * SimpleTransConstants.PercentageToDecimal) / SimpleTransConstants.PercentageToDecimal;
			SimpleTransDebug.Log($"Settings loaded - cisRate: {cisRate:F3}, transManCarryRate: {transManCarryRate:F3}", 2);
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
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error loading settings, using defaults: {ex}");
			// Set all defaults
			cisRate = SimpleTransConstants.DefaultCisRate;
			transManCarryRate = SimpleTransConstants.DefaultTransManCarryPercent;
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

	#region Helper Methods

	/// <summary>
	/// Safely parses a float value with fallback to default
	/// </summary>
	/// <param name="value">The string value to parse</param>
	/// <param name="defaultValue">The default value if parsing fails</param>
	/// <returns>The parsed float or default value</returns>
	public static float TryParseFloat(string value, float defaultValue)
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
	public static bool TryParseBool(string value, bool defaultValue)
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
}
