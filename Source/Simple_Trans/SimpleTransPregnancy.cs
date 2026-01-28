using System;
using System.Linq;
using RimWorld;
using Verse;

namespace Simple_Trans;

/// <summary>
/// Pregnancy-specific logic for Simple Trans mod
/// Contains methods for creating and managing pregnancies
/// </summary>
public static class SimpleTransPregnancy
{
	#region Enums

	/// <summary>
	/// Ability types for sterilization conversion
	/// </summary>
	public enum AbilityType
	{
		Carry,
		Sire
	}

	#endregion

	#region Pregnancy Creation Methods

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

			// Determine reproductive roles using unified logic
			Pawn sirer = null;
			Pawn carrier = null;

			if (pawn2 != null)
			{
				// Two-pawn scenario: use unified role assignment
				SimpleTransHediffs.DetermineRoles(pawn1, pawn2, out carrier, out sirer);
			}
			else
			{
				// Single-pawn scenario: pawn1 must be the carrier
				if (SimpleTransHediffs.CanCarry(pawn1))
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
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error in TryCreatePregnancy: {ex}");
			return false;
		}
	}

	/// <summary>
	/// Internal method that creates the actual pregnancy hediff
	/// </summary>
	/// <param name="carrier">The pawn who will carry the pregnancy</param>
	/// <param name="sirer">The pawn who sired the pregnancy (can be null)</param>
	/// <param name="progressOverride">Specific pregnancy progress (null for immediate pregnancy)</param>
	/// <param name="isBackgroundPregnancy">Whether this is a background pregnancy (for world gen)</param>
	/// <param name="showIncompatibilityMessage">If true, shows user message when genes are incompatible</param>
	/// <returns>True if pregnancy was successfully created</returns>
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
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error creating pregnancy hediff for {carrier?.Name?.ToStringShort ?? "unknown"}: {ex}");
			return false;
		}
	}

	#endregion
}
