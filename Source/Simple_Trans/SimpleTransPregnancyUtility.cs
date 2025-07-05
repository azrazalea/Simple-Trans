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
		return pawn.health.hediffSet.HasHediff(canCarryDef, false);
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
		return pawn.health.hediffSet.HasHediff(canSireDef, false);
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

			// Determine gender identity and reproductive capabilities based on Simple Trans logic
			// Binary genders (Male/Female) get assigned trans/cis status; non-binary pawns are handled differently
			bool hasBinaryGender = pawn.gender == Gender.Male || pawn.gender == Gender.Female;
			
			// Randomly determine if this pawn is transgender based on configured rates
			bool isTransgender = DecideTransgender(pawn);
			
			// Reproductive capability assignment follows trans-inclusive logic:
			// - Carrying ability: Cis females (F->F) OR Trans males (M->F identity, can carry)
			// - Siring ability: Cis males (M->M) OR Trans females (F->M identity, can sire)
			// This allows for realistic reproductive diversity while respecting gender identity
			bool canCarry = (pawn.gender == Gender.Female && !isTransgender) || (pawn.gender == Gender.Male && isTransgender);
			bool canSire = (pawn.gender == Gender.Male && !isTransgender) || (pawn.gender == Gender.Female && isTransgender);

			// Apply gender identity hediffs (visible in health tab)
			if (isTransgender)
			{
				SetTrans(pawn);
			}
			else if (hasBinaryGender)
			{
				// Only apply cisgender hediff to binary genders
				SetCis(pawn);
			}
			
			// Apply reproductive capability hediffs
			// The removeSire/removeCarry logic ensures pawns don't have conflicting abilities
			// unless they're transgender (allowing for intersex/non-standard combinations)
			if (canCarry)
			{
				SetCarry(pawn, !isTransgender || !canSire);
			}
			if (canSire)
			{
				SetSire(pawn, !isTransgender || !canCarry);
			}

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

			// Parse settings with error handling and defaults
			cisRate = TryParseFloat(cisPercentSetting, SimpleTransConstants.DefaultCisRate * SimpleTransConstants.PercentageToDecimal) / SimpleTransConstants.PercentageToDecimal;
			mCarryRate = TryParseFloat(mCarryPercentSetting, SimpleTransConstants.DefaultMaleCarryRate * SimpleTransConstants.PercentageToDecimal) / SimpleTransConstants.PercentageToDecimal;
			fSireRate = TryParseFloat(fSirePercentSetting, SimpleTransConstants.DefaultFemaleSireRate * SimpleTransConstants.PercentageToDecimal) / SimpleTransConstants.PercentageToDecimal;
			nCarryRate = TryParseFloat(nCarryPercentSetting, SimpleTransConstants.DefaultNonBinaryCarryRate * SimpleTransConstants.PercentageToDecimal) / SimpleTransConstants.PercentageToDecimal;
			genesAreAgab = TryParseBool(genesAreAgabSetting, true);
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
			
			if (pawn.gender == Gender.Male && isTransgender)
			{
				SetTrans(pawn);
				if (pawn.gender == Gender.Male && Rand.Range(0f, 1f) <= mCarryRate)
				{
					SetCarry(pawn);
				}
				else if (pawn.gender == Gender.Male && Rand.Range(0f, 1f) > mCarryRate)
				{
					SetSire(pawn);
				}
			}
			else if (pawn.gender == Gender.Female && isTransgender)
			{
				SetTrans(pawn);
				if (pawn.gender == Gender.Female && Rand.Range(0f, 1f) <= fSireRate)
				{
					SetSire(pawn);
				}
				else if (pawn.gender == Gender.Female && Rand.Range(0f, 1f) > fSireRate)
				{
					SetCarry(pawn);
				}
			}
			
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
				
				// Always grant carrying ability for forceFemale genes
				SetCarry(pawn, removeSire: false);
			}
			
			// Handle genes that force male reproductive capabilities
			if (modExtension.forceMale)
			{
				// Gene forces male reproductive role, but gender identity can still vary
				// Logic is inverted: if NOT above cis rate, assign male gender (cis male)
				// If above cis rate, assign female gender (trans female)
				pawn.gender = (!(Rand.Range(0f, 1f) > cisRate)) ? Gender.Male : Gender.Female;
				
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
				
				// Always grant siring ability for forceMale genes
				SetSire(pawn, removeCarry: false);
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
	/// Removes all gender-related hediffs from a pawn
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
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error clearing gender hediffs for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}
	
	#endregion
}