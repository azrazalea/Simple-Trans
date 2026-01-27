using System;
using System.Linq;
using RimWorld;
using Verse;

namespace Simple_Trans;

/// <summary>
/// Gender modification methods for Simple Trans mod
/// Contains methods for setting gender identity and reproductive capabilities
/// </summary>
public static class GenderAssignment
{
	#region Gender Identity Methods

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
			if (!pawn.health.hediffSet.HasHediff(SimpleTransHediffs.transDef, false))
			{
				pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(SimpleTransHediffs.cisDef));
				pawn.health.GetOrAddHediff(SimpleTransHediffs.transDef);
			}
		}
		catch (Exception ex)
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
			if (SimpleTransHediffs.IsEnby(pawn))
			{
				Log.Warning("[Simple Trans] Setting non-binary pawn to cisgender - this may cause unexpected behavior.");
			}

			if (!pawn.health.hediffSet.HasHediff(SimpleTransHediffs.cisDef, false))
			{
				pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(SimpleTransHediffs.transDef));
				pawn.health.GetOrAddHediff(SimpleTransHediffs.cisDef);
			}
		}
		catch (Exception ex)
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
	/// <param name="clearSterilization">Whether to clear sterilization hediffs</param>
	public static void SetCarry(Pawn pawn, bool removeSire = false, bool clearSterilization = false)
	{
		if (pawn?.health?.hediffSet == null)
		{
			Log.Error("[Simple Trans] SetCarry called with null pawn or missing health data");
			return;
		}

		try
		{
			if (!pawn.health.hediffSet.HasHediff(SimpleTransHediffs.canCarryDef, false))
			{
				pawn.health.GetOrAddHediff(SimpleTransHediffs.canCarryDef);
				if ((removeSire || pawn.gender == Gender.Female) && pawn.health.hediffSet.HasHediff(SimpleTransHediffs.canSireDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(SimpleTransHediffs.canSireDef));
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
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error setting carry hediff for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}

	/// <summary>
	/// Grants a pawn the ability to sire offspring
	/// </summary>
	/// <param name="pawn">The pawn to modify</param>
	/// <param name="removeCarry">Whether to remove carrying ability (for exclusive reproductive roles)</param>
	/// <param name="clearSterilization">Whether to clear sterilization hediffs</param>
	public static void SetSire(Pawn pawn, bool removeCarry = false, bool clearSterilization = false)
	{
		if (pawn?.health?.hediffSet == null)
		{
			Log.Error("[Simple Trans] SetSire called with null pawn or missing health data");
			return;
		}

		try
		{
			if (!pawn.health.hediffSet.HasHediff(SimpleTransHediffs.canSireDef, false))
			{
				pawn.health.GetOrAddHediff(SimpleTransHediffs.canSireDef);
				if ((removeCarry || pawn.gender == Gender.Male) && pawn.health.hediffSet.HasHediff(SimpleTransHediffs.canCarryDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(SimpleTransHediffs.canCarryDef));
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
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error setting sire hediff for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
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
				if (pawn.health.hediffSet.HasHediff(SimpleTransHediffs.cisDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(SimpleTransHediffs.cisDef));
				}
				if (pawn.health.hediffSet.HasHediff(SimpleTransHediffs.transDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(SimpleTransHediffs.transDef));
				}
			}

			// Remove reproductive capability hediffs if requested
			if (clearCapabilities)
			{
				if (pawn.health.hediffSet.HasHediff(SimpleTransHediffs.canCarryDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(SimpleTransHediffs.canCarryDef));
				}
				if (pawn.health.hediffSet.HasHediff(SimpleTransHediffs.canSireDef, false))
				{
					pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(SimpleTransHediffs.canSireDef));
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
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error clearing gender hediffs for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
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
				if (SimpleTransSettings.enableProsthetics && Rand.Range(0f, 1f) < SimpleTransSettings.prostheticCarryRate)
				{
					// Apply prosthetic modifier
					if (Rand.Range(0f, 1f) < SimpleTransSettings.bionicUpgradeRate)
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
					if (Rand.Range(0f, 1f) < SimpleTransSettings.carrySterilizationRate)
					{
						if (Rand.Range(0f, 1f) < SimpleTransSettings.reversibleSterilizationRate)
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
				if (SimpleTransSettings.enableProsthetics && Rand.Range(0f, 1f) < SimpleTransSettings.prostheticSireRate)
				{
					// Apply prosthetic modifier
					if (Rand.Range(0f, 1f) < SimpleTransSettings.bionicUpgradeRate)
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
					if (Rand.Range(0f, 1f) < SimpleTransSettings.sireSterilizationRate)
					{
						if (Rand.Range(0f, 1f) < SimpleTransSettings.reversibleSterilizationRate)
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
		catch (Exception ex)
		{
			Log.Error($"[Simple Trans] Error applying reproductive capabilities for {pawn?.Name?.ToStringShort ?? "unknown"}: {ex}");
		}
	}

	#endregion
}
