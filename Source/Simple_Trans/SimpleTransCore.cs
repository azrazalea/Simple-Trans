using System;
using RimWorld;
using VEF.Genes;
using Verse;

namespace Simple_Trans;

/// <summary>
/// Core gender determination logic for Simple Trans mod
/// Contains the main validation and gender assignment methods
/// </summary>
public static class SimpleTransCore
{
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
			GenderAssignment.ClearGender(pawn, clearIdentity: true, clearCapabilities: !isPregnant);

			// If pregnant, ensure they have carry ability (they must have it to be pregnant)
			if (isPregnant && !SimpleTransHediffs.CanCarry(pawn))
			{
				GenderAssignment.SetCarry(pawn, false, clearSterilization: true);
				SimpleTransDebug.Log($"Added carry ability to pregnant pawn {pawn.Name?.ToStringShort}", 2);
			}

			// Use unified determination logic
			var (isTransgender, canCarry, canSire) = DetermineGenderAndCapabilities(pawn);

			// Apply identity hediff
			if (isTransgender)
			{
				GenderAssignment.SetTrans(pawn);
			}
			else
			{
				GenderAssignment.SetCis(pawn);
			}

			// Apply reproductive capabilities with prosthetics and sterilization
			// BUT skip if pregnant to avoid interfering with ongoing pregnancy
			if (!isPregnant)
			{
				GenderAssignment.ApplyReproductiveCapabilities(pawn, canCarry, canSire);
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
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error in ValidateOrSetGender for pawn {pawn?.Name?.ToStringShort ?? "null"}: {ex}");
		}
	}

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
			if (specialCaseRoll < SimpleTransSettings.enbyBothRate)
			{
				// Both abilities
				canCarry = true;
				canSire = true;
			}
			else if (specialCaseRoll < SimpleTransSettings.enbyBothRate + SimpleTransSettings.enbyNeitherRate)
			{
				// Neither ability
				canCarry = false;
				canSire = false;
			}
			else
			{
				// Single ability - use fresh roll and enbyCarryRate to determine which one
				float abilityRoll = Rand.Range(0f, 1f);
				if (abilityRoll < SimpleTransSettings.enbyCarryRate)
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
			isTransgender = Rand.Range(0f, 1f) > SimpleTransSettings.cisRate;

			if (isTransgender)
			{
				// Transgender binary logic
				float specialCaseRoll = Rand.Range(0f, 1f);
				if (specialCaseRoll < SimpleTransSettings.transBothRate)
				{
					// Both abilities
					canCarry = true;
					canSire = true;
				}
				else if (specialCaseRoll < SimpleTransSettings.transBothRate + SimpleTransSettings.transNeitherRate)
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
						canCarry = abilityRoll < SimpleTransSettings.transManCarryRate;
						canSire = !canCarry;
					}
					else if (pawn.gender == Gender.Female)
					{
						canSire = abilityRoll < SimpleTransSettings.transWomanSireRate;
						canCarry = !canSire;
					}
				}
			}
			else
			{
				// Cisgender logic
				float specialCaseRoll = Rand.Range(0f, 1f);
				if (specialCaseRoll < SimpleTransSettings.cisBothRate)
				{
					// Both abilities (rare)
					canCarry = true;
					canSire = true;
				}
				else if (specialCaseRoll < SimpleTransSettings.cisBothRate + SimpleTransSettings.cisNeitherRate)
				{
					// Neither ability
					canCarry = false;
					canSire = false;
				}
				else if (pawn.gender == Gender.Male)
				{
					// Fresh roll for cis man ability determination
					float abilityRoll = Rand.Range(0f, 1f);
					canCarry = abilityRoll < SimpleTransSettings.cisManCarryRate;
					canSire = !canCarry;
				}
				else if (pawn.gender == Gender.Female)
				{
					// Fresh roll for cis woman ability determination
					float abilityRoll = Rand.Range(0f, 1f);
					canSire = abilityRoll < SimpleTransSettings.cisWomanSireRate;
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
			if (modExtension == null || !SimpleTransSettings.genesAreAgab)
			{
				// Skip processing if no extension or genes don't represent AGAB (assigned gender at birth)
				return;
			}

			// Special non-binary handling
			if (SimpleTransHediffs.IsEnby(pawn) && (modExtension.forceFemale || modExtension.forceMale))
			{
				GenderAssignment.SetTrans(pawn);

				if (Rand.Range(0f, 1f) < SimpleTransSettings.enbyBothRate)
				{
					GenderAssignment.SetCarry(pawn, false);
					GenderAssignment.SetSire(pawn, false);
				}
				else if (Rand.Range(0f, 1f) < SimpleTransSettings.enbyNeitherRate)
				{
					// Don't set carry or sire
				}
				else
				{
					if (modExtension.forceFemale)
					{
						GenderAssignment.SetCarry(pawn, true); // Force carry ability
					}
					else
					{
						GenderAssignment.SetSire(pawn, true); // Force sire ability
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
				pawn.gender = (Rand.Range(0f, 1f) > SimpleTransSettings.cisRate) ? Gender.Male : Gender.Female;

				// Clear any existing reproductive hediffs first (but preserve identity as we're about to set it)
				GenderAssignment.ClearGender(pawn, clearIdentity: false, clearCapabilities: true);

				if (pawn.gender == Gender.Male)
				{
					// Trans man: masculine gender identity, can carry pregnancies
					GenderAssignment.SetTrans(pawn);

					if (Rand.Range(0f, 1f) < SimpleTransSettings.transBothRate)
					{
						// Rare case: trans male with both abilities
						GenderAssignment.SetCarry(pawn, false);
						GenderAssignment.SetSire(pawn, false);
					}
					else if (Rand.Range(0f, 1f) < SimpleTransSettings.transNeitherRate)
					{
						// Don't set carry or sire
						return;
					}
					else
					{
						// Standard: always grant carrying ability for forceFemale genes
						GenderAssignment.SetCarry(pawn, true);
					}
				}
				else if (pawn.gender == Gender.Female)
				{
					// Cis woman: feminine gender identity, can carry pregnancies
					GenderAssignment.SetCis(pawn);
					if (Rand.Range(0f, 1f) < SimpleTransSettings.cisBothRate)
					{
						// Rare case: cis female with both abilities
						GenderAssignment.SetCarry(pawn, false);
						GenderAssignment.SetSire(pawn, false);
					}
					else
					{
						GenderAssignment.SetCarry(pawn, true);
					}
				}
				else // Secret fourth thing?
				{
					GenderAssignment.SetCarry(pawn, true);
				}
			}

			// Handle genes that force male reproductive capabilities
			if (modExtension.forceMale)
			{
				// Gene forces male reproductive role, but gender identity can still vary
				// If above cis rate, assign female gender (trans female)
				pawn.gender = (Rand.Range(0f, 1f) > SimpleTransSettings.cisRate) ? Gender.Female : Gender.Male;

				// Clear any existing reproductive hediffs first (but preserve identity as we're about to set it)
				GenderAssignment.ClearGender(pawn, clearIdentity: false, clearCapabilities: true);

				if (pawn.gender == Gender.Female)
				{
					// Trans woman: feminine gender identity, can sire pregnancies
					GenderAssignment.SetTrans(pawn);
					if (Rand.Range(0f, 1f) < SimpleTransSettings.transBothRate)
					{
						// Rare case: trans woman with both abilities
						GenderAssignment.SetCarry(pawn, false);
						GenderAssignment.SetSire(pawn, false);
					}
					else if (Rand.Range(0f, 1f) < SimpleTransSettings.transNeitherRate)
					{
						// Don't set carry or sire
						return;
					}
					else
					{
						// Standard: always grant siring ability for forceMale genes
						GenderAssignment.SetSire(pawn, true);
					}
				}
				else if (pawn.gender == Gender.Male)
				{
					// Cis man: masculine gender identity, can sire pregnancies
					GenderAssignment.SetCis(pawn);
					if (Rand.Range(0f, 1f) < SimpleTransSettings.cisBothRate)
					{
						// Rare case: cis man with both abilities
						GenderAssignment.SetCarry(pawn, false);
						GenderAssignment.SetSire(pawn, false);
					}
					else
					{
						GenderAssignment.SetSire(pawn, true);
					}
				}
				else // Secret fourth thing?
				{
					GenderAssignment.SetSire(pawn, true);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error in ValidateOrSetGenderWithGenes for {pawn?.Name?.ToStringShort ?? "unknown"} with gene {gene?.def?.defName ?? "unknown"}: {ex}");
		}
	}

	#endregion

	#region Sterilization Conversion

	/// <summary>
	/// Core conversion logic for vanilla Sterilized hediff to capability-specific sterilization
	/// </summary>
	/// <param name="pawn">The pawn to convert</param>
	/// <param name="forceCarryingSterilization">If true, force carrying sterilization regardless of logic</param>
	/// <param name="forceSiringSterilization">If true, force siring sterilization regardless of logic</param>
	/// <param name="useOppositeCapabilityLogic">If true, use opposite capability logic (for adding abilities)</param>
	/// <param name="targetAbility">The ability being added (only used with opposite capability logic)</param>
	/// <returns>True if conversion was performed, false if no vanilla sterilized hediff was found</returns>
	public static bool ConvertVanillaSterilizedHediffCore(Pawn pawn, bool forceCarryingSterilization = false, bool forceSiringSterilization = false, bool useOppositeCapabilityLogic = false, SimpleTransPregnancy.AbilityType? targetAbility = null)
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
					if (targetAbility == SimpleTransPregnancy.AbilityType.Carry)
					{
						shouldSterilizeSire = true;
					}
					else if (targetAbility == SimpleTransPregnancy.AbilityType.Sire)
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
					bool isTransgender = pawn.health.hediffSet.HasHediff(SimpleTransHediffs.transDef);

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
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error converting vanilla sterilized hediff for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
			return false;
		}
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
