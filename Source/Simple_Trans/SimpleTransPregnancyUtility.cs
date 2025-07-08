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
	/// Rate at which males can carry pregnancies
	/// </summary>
	public static float mCarryRate;
	
	/// <summary>
	/// Rate at which females can sire offspring
	/// </summary>
	public static float fSireRate;
	
	/// <summary>
	/// Rate at which non-binary pawns can carry pregnancies
	/// </summary>
	public static float nCarryRate;
	
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
	/// Rate at which cis males can carry
	/// </summary>
	public static float cisMCarryRate;
	
	/// <summary>
	/// Rate at which cis females can sire
	/// </summary>
	public static float cisFSireRate;
	
	/// <summary>
	/// Rate at which cis people have both abilities
	/// </summary>
	public static float cisBothRate;
	
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
		if (pawn?.health?.hediffSet == null)
		{
			SimpleTransDebug.Log("CanCarry called with null pawn or missing health data", 1);
			return false;
		}
		
		// Check if pawn has carry capability
		if (!pawn.health.hediffSet.HasHediff(canCarryDef, false))
		{
			return false;
		}
		
		// Check for sterilization that blocks carrying
		if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Sterilized) != null)
		{
			return false; // Vanilla sterilized blocks all reproduction
		}
		
		var sterilizedCarryDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedCarry");
		if (sterilizedCarryDef != null && pawn.health.hediffSet.HasHediff(sterilizedCarryDef, false))
		{
			return false; // Specifically sterilized for carrying
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
		if (pawn?.health?.hediffSet == null)
		{
			SimpleTransDebug.Log("CanSire called with null pawn or missing health data", 1);
			return false;
		}
		
		// Check if pawn has sire capability
		if (!pawn.health.hediffSet.HasHediff(canSireDef, false))
		{
			return false;
		}
		
		// Check for sterilization that blocks siring
		if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Sterilized) != null)
		{
			return false; // Vanilla sterilized blocks all reproduction
		}
		
		var sterilizedSireDef = DefDatabase<HediffDef>.GetNamedSilentFail("SterilizedSire");
		if (sterilizedSireDef != null && pawn.health.hediffSet.HasHediff(sterilizedSireDef, false))
		{
			return false; // Specifically sterilized for siring
		}
		
		return true;
	}
	
	#endregion

	#region Core Gender Logic
	
	/// <summary>
	/// Main entry point for validating or setting gender identity and reproductive capabilities
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

			// Load mod settings
			LoadSettings();

			// Determine gender identity and reproductive capabilities
			bool hasBinaryGender = pawn.gender == Gender.Male || pawn.gender == Gender.Female;
			
			// First determine if transgender
			bool isTransgender = Rand.Range(0f, 1f) > cisRate;
			
			// Apply gender identity hediff
			if (isTransgender)
			{
				SetTrans(pawn);
			}
			else if (hasBinaryGender)
			{
				SetCis(pawn);
			}
			
			// Now determine reproductive abilities based on gender and trans status
			bool canCarry = false;
			bool canSire = false;
			
			// Handle non-binary pawns separately (if NBG mod adds them)
			if (pawn.gender != Gender.Male && pawn.gender != Gender.Female)
			{
				// Non-binary pawn - use nCarryRate setting
				canCarry = Rand.Range(0f, 1f) <= nCarryRate;
				canSire = !canCarry; // Rest get sire ability
			}
			else if (isTransgender)
			{
				// Check for special cases first
				float roll = Rand.Range(0f, 1f);
				if (roll < transBothRate)
				{
					// Both abilities
					canCarry = true;
					canSire = true;
				}
				else if (roll < transBothRate + transNeitherRate)
				{
					// Neither ability
					canCarry = false;
					canSire = false;
				}
				else
				{
					// Standard trans logic
					if (pawn.gender == Gender.Male)
					{
						canCarry = Rand.Range(0f, 1f) <= mCarryRate;
						canSire = !canCarry;
					}
					else if (pawn.gender == Gender.Female)
					{
						canSire = Rand.Range(0f, 1f) <= fSireRate;
						canCarry = !canSire;
					}
				}
			}
			else
			{
				// Cisgender logic
				float roll = Rand.Range(0f, 1f);
				if (roll < cisBothRate)
				{
					// Both abilities (rare)
					canCarry = true;
					canSire = true;
				}
				else if (pawn.gender == Gender.Male)
				{
					canSire = true;
					canCarry = Rand.Range(0f, 1f) <= cisMCarryRate;
				}
				else if (pawn.gender == Gender.Female)
				{
					canCarry = true;
					canSire = Rand.Range(0f, 1f) <= cisFSireRate;
				}
			}
			
			// Apply reproductive abilities
			if (canCarry)
			{
				SetCarry(pawn, false);
			}
			if (canSire)
			{
				SetSire(pawn, false);
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
	/// Loads mod settings from the settings manager
	/// </summary>
	private static void LoadSettings()
	{
		try
		{
			// Load settings with null safety
			string cisPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "cisPercent");
			string mCarryPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "mCarryPercent");
			string fSirePercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "fSirePercent");
			string nCarryPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "nCarryPercent");
			string genesAreAgabSetting = SettingsManager.GetSetting("runaway.simpletrans", "genesAreAgab");
			string enableOrganTransplantsSetting = SettingsManager.GetSetting("runaway.simpletrans", "enableOrganTransplants");
			string enableProstheticsSetting = SettingsManager.GetSetting("runaway.simpletrans", "enableProsthetics");
			string transBothPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "transBothPercent");
			string transNeitherPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "transNeitherPercent");
			string cisMCarryPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "cisMCarryPercent");
			string cisFSirePercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "cisFSirePercent");
			string cisBothPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "cisBothPercent");

			// Parse settings with error handling and defaults
			cisRate = TryParseFloat(cisPercentSetting, SimpleTransConstants.DefaultCisRate * SimpleTransConstants.PercentageToDecimal) / SimpleTransConstants.PercentageToDecimal;
			mCarryRate = TryParseFloat(mCarryPercentSetting, SimpleTransConstants.DefaultMaleCarryRate * SimpleTransConstants.PercentageToDecimal) / SimpleTransConstants.PercentageToDecimal;
			fSireRate = TryParseFloat(fSirePercentSetting, SimpleTransConstants.DefaultFemaleSireRate * SimpleTransConstants.PercentageToDecimal) / SimpleTransConstants.PercentageToDecimal;
			nCarryRate = TryParseFloat(nCarryPercentSetting, SimpleTransConstants.DefaultNonBinaryCarryRate * SimpleTransConstants.PercentageToDecimal) / SimpleTransConstants.PercentageToDecimal;
			genesAreAgab = TryParseBool(genesAreAgabSetting, true);
			enableOrganTransplants = TryParseBool(enableOrganTransplantsSetting, true);
			enableProsthetics = TryParseBool(enableProstheticsSetting, true);
			transBothRate = TryParseFloat(transBothPercentSetting, 5f) / SimpleTransConstants.PercentageToDecimal;
			transNeitherRate = TryParseFloat(transNeitherPercentSetting, 5f) / SimpleTransConstants.PercentageToDecimal;
			cisMCarryRate = TryParseFloat(cisMCarryPercentSetting, 1f) / SimpleTransConstants.PercentageToDecimal;
			cisFSireRate = TryParseFloat(cisFSirePercentSetting, 1f) / SimpleTransConstants.PercentageToDecimal;
			cisBothRate = TryParseFloat(cisBothPercentSetting, 0f) / SimpleTransConstants.PercentageToDecimal;
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error loading settings, using defaults: {ex}");
			// Set all defaults
			cisRate = SimpleTransConstants.DefaultCisRate;
			mCarryRate = SimpleTransConstants.DefaultMaleCarryRate;
			fSireRate = SimpleTransConstants.DefaultFemaleSireRate;
			nCarryRate = SimpleTransConstants.DefaultNonBinaryCarryRate;
			genesAreAgab = true;
			enableOrganTransplants = true;
			enableProsthetics = true;
			transBothRate = 0.05f;
			transNeitherRate = 0.05f;
			cisMCarryRate = 0.01f;
			cisFSireRate = 0.01f;
			cisBothRate = 0f;
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
	public static void SetCarry(Pawn pawn, bool removeSire = false)
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
	public static void SetSire(Pawn pawn, bool removeCarry = false)
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
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error setting sire hediff for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}
	
	#endregion

	#region Decision Logic
	
	/// <summary>
	/// Determines if a pawn should be transgender based on configuration rates
	/// Legacy method for NBG patches - main logic now in ValidateOrSetGender
	/// </summary>
	/// <param name="pawn">The pawn to evaluate</param>
	/// <returns>True if the pawn should be transgender</returns>
	public static bool DecideTransgender(Pawn pawn)
	{
		if (pawn == null)
		{
			Log.Error("[Simple Trans] DecideTransgender called with null pawn");
			return false;
		}
		
		try
		{
			bool isTransgender = Rand.Range(0f, 1f) > cisRate;
			SimpleTransDebug.Log("Transgender = " + isTransgender, 3);
			return isTransgender;
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error in DecideTransgender for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
			return false;
		}
	}

	/// <summary>
	/// Determines and sets the reproductive type for a pawn based on their gender identity
	/// </summary>
	/// <param name="pawn">The pawn to process</param>
	/// <returns>True if the pawn can carry pregnancies</returns>
	public static bool DecideReproductionType(Pawn pawn)
	{
		bool isTransgender = pawn.health.hediffSet.HasHediff(transDef, false);
		bool canCarry = pawn.health.hediffSet.HasHediff(canCarryDef, false);
		bool canSire = pawn.health.hediffSet.HasHediff(canSireDef, false);
		
		SimpleTransDebug.Log("Checking reproduction type for " + pawn.Name, 3);
		
		if (pawn.gender == Gender.Male && isTransgender && Rand.Range(0f, 1f) <= mCarryRate)
		{
			canCarry = true;
			canSire = false;
		}
		else if (pawn.gender == Gender.Male && isTransgender && Rand.Range(0f, 1f) > mCarryRate)
		{
			canCarry = false;
			canSire = true;
		}
		else if (pawn.gender == Gender.Female && isTransgender && Rand.Range(0f, 1f) <= fSireRate)
		{
			canCarry = false;
			canSire = true;
		}
		else if (pawn.gender == Gender.Female && isTransgender && Rand.Range(0f, 1f) > fSireRate)
		{
			canCarry = true;
			canSire = false;
		}
		else if (pawn.gender == Gender.Male && !isTransgender)
		{
			canCarry = false;
			canSire = true;
		}
		else if (pawn.gender == Gender.Female && !isTransgender)
		{
			canCarry = true;
			canSire = false;
		}
		else if (pawn.gender != Gender.Male && pawn.gender != Gender.Female)
		{
			// Non-binary pawn - use nCarryRate setting
			canCarry = Rand.Range(0f, 1f) <= nCarryRate;
			canSire = !canCarry;
		}
		
		SimpleTransDebug.Log("carry = " + canCarry + " sire = " + canSire, 3);
		
		if (canCarry)
		{
			SetCarry(pawn);
		}
		if (canSire)
		{
			SetSire(pawn);
		}
		
		return canCarry;
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
			
			// Handle genes that force female reproductive capabilities
			if (modExtension.forceFemale)
			{
				// Gene forces female reproductive role, but gender identity can still vary
				// If transgender rate applies, pawn becomes trans male (M gender, F reproductive role)
				// Otherwise becomes cis female (F gender, F reproductive role)
				pawn.gender = (Rand.Range(0f, 1f) > cisRate) ? Gender.Male : Gender.Female;
				
				// Clear any existing reproductive hediffs first
				ClearGender(pawn);
				
				if (pawn.gender == Gender.Male)
				{
					// Trans male: male gender identity, can carry pregnancies
					SetTrans(pawn);
				}
				else if (pawn.gender == Gender.Female)
				{
					// Cis female: female gender identity, can carry pregnancies
					SetCis(pawn);
				}
				
				// Check for special variations even with forced genes
				float roll = Rand.Range(0f, 1f);
				if (roll < cisBothRate && pawn.gender == Gender.Female)
				{
					// Rare case: cis female with both abilities
					SetCarry(pawn, false);
					SetSire(pawn, false);
				}
				else if (roll < transBothRate && pawn.gender == Gender.Male)
				{
					// Rare case: trans male with both abilities
					SetCarry(pawn, false);
					SetSire(pawn, false);
				}
				else
				{
					// Standard: always grant carrying ability for forceFemale genes
					SetCarry(pawn, false);
				}
			}
			
			// Handle genes that force male reproductive capabilities
			if (modExtension.forceMale)
			{
				// Gene forces male reproductive role, but gender identity can still vary
				// Logic is inverted: if NOT above cis rate, assign male gender (cis male)
				// If above cis rate, assign female gender (trans female)
				pawn.gender = (!(Rand.Range(0f, 1f) > cisRate)) ? Gender.Male : Gender.Female;
				
				// Clear any existing reproductive hediffs first
				ClearGender(pawn);
				
				if (pawn.gender == Gender.Female)
				{
					// Trans female: female gender identity, can sire offspring
					SetTrans(pawn);
				}
				else
				{
					// Cis male: male gender identity, can sire offspring
					SetCis(pawn);
				}
				
				// Check for special variations even with forced genes
				float roll = Rand.Range(0f, 1f);
				if (roll < cisBothRate && pawn.gender == Gender.Male)
				{
					// Rare case: cis male with both abilities
					SetCarry(pawn, false);
					SetSire(pawn, false);
				}
				else if (roll < transBothRate && pawn.gender == Gender.Female)
				{
					// Rare case: trans female with both abilities
					SetCarry(pawn, false);
					SetSire(pawn, false);
				}
				else
				{
					// Standard: always grant siring ability for forceMale genes
					SetSire(pawn, false);
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
	/// Removes all gender-related and reproductive hediffs from a pawn
	/// </summary>
	/// <param name="pawn">The pawn to clear</param>
	public static void ClearGender(Pawn pawn)
	{
		if (pawn?.health?.hediffSet == null)
		{
			Log.Error("[Simple Trans] ClearGender called with null pawn or missing health data");
			return;
		}
		
		try
		{
			// Remove all gender-related hediffs
			if (pawn.health.hediffSet.HasHediff(cisDef, false))
			{
				pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(cisDef));
			}
			if (pawn.health.hediffSet.HasHediff(transDef, false))
			{
				pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(transDef));
			}
			if (pawn.health.hediffSet.HasHediff(canCarryDef, false))
			{
				pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(canCarryDef));
			}
			if (pawn.health.hediffSet.HasHediff(canSireDef, false))
			{
				pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(canSireDef));
			}
			
			// Also remove all prosthetic and sterilization hediffs
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
	
	#endregion
}