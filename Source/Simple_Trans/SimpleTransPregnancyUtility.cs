using RimWorld;
using VEF.Genes;
using Verse;
using XmlExtensions;

namespace Simple_Trans;

public static class SimpleTransPregnancyUtility
{
	public static float cisRate;

	public static float mCarryRate;

	public static float fSireRate;

	public static float nCarryRate;

	public static bool genesAreAgab;

	public static HediffDef cisDef = HediffDef.Named("Cisgender");

	public static HediffDef transDef = HediffDef.Named("Transgender");

	public static HediffDef canCarryDef = HediffDef.Named("PregnancyCarry");

	public static HediffDef canSireDef = HediffDef.Named("PregnancySire");

	public static bool CanCarry(Pawn pawn)
	{
		return pawn.health.hediffSet.HasHediff(canCarryDef, false);
	}

	public static bool CanSire(Pawn pawn)
	{
		return pawn.health.hediffSet.HasHediff(canSireDef, false);
	}

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

			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Invalid comparison between Unknown and I4
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Invalid comparison between Unknown and I4
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Invalid comparison between Unknown and I4
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Invalid comparison between Unknown and I4
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Invalid comparison between Unknown and I4
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Invalid comparison between Unknown and I4
			bool flag = (int)pawn.gender == 1 || (int)pawn.gender == 2;
			bool flag2 = DecideTransgender(pawn);
			bool flag3 = ((int)pawn.gender == 2 && !flag2) || ((int)pawn.gender == 1 && flag2);  // Can carry: cis female OR trans male
			bool flag4 = ((int)pawn.gender == 1 && !flag2) || ((int)pawn.gender == 2 && flag2);  // Can sire: cis male OR trans female

			// Load settings with null safety
			string cisPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "cisPercent");
			string mCarryPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "mCarryPercent");
			string fSirePercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "fSirePercent");
			string nCarryPercentSetting = SettingsManager.GetSetting("runaway.simpletrans", "nCarryPercent");
			string genesAreAgabSetting = SettingsManager.GetSetting("runaway.simpletrans", "genesAreAgab");

			cisRate = !string.IsNullOrEmpty(cisPercentSetting) ? float.Parse(cisPercentSetting) / 100f : 0.9f;
			mCarryRate = !string.IsNullOrEmpty(mCarryPercentSetting) ? float.Parse(mCarryPercentSetting) / 100f : 1.0f;
			fSireRate = !string.IsNullOrEmpty(fSirePercentSetting) ? float.Parse(fSirePercentSetting) / 100f : 1.0f;
			nCarryRate = !string.IsNullOrEmpty(nCarryPercentSetting) ? float.Parse(nCarryPercentSetting) / 100f : 0.5f;
			genesAreAgab = !string.IsNullOrEmpty(genesAreAgabSetting) ? bool.Parse(genesAreAgabSetting) : true;

			if (flag2)
			{
				SetTrans(pawn);
			}
			else if (flag)
			{
				SetCis(pawn);
			}
			if (flag3)
			{
				SetCarry(pawn, !flag2 || !flag4);
			}
			if (flag4)
			{
				SetSire(pawn, !flag2 || !flag3);
			}

			// Process genes with additional safety check
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

	public static void SetTrans(Pawn pawn)
	{
		if (!pawn.health.hediffSet.HasHediff(transDef, false))
		{
			pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(cisDef));
			pawn.health.GetOrAddHediff(transDef);
		}
	}

	public static void SetCis(Pawn pawn)
	{
		if (!pawn.health.hediffSet.HasHediff(cisDef, false))
		{
			pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(transDef));
			pawn.health.GetOrAddHediff(cisDef);
		}
	}

	public static void SetCarry(Pawn pawn, bool removeSire = false)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Invalid comparison between Unknown and I4
		if (!pawn.health.hediffSet.HasHediff(canCarryDef, false))
		{
			pawn.health.GetOrAddHediff(canCarryDef);
			if ((removeSire || (int)pawn.gender == 2) && pawn.health.hediffSet.HasHediff(canSireDef, false))
			{
				pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(canSireDef));
			}
		}
	}

	public static void SetSire(Pawn pawn, bool removeCarry = false)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Invalid comparison between Unknown and I4
		if (!pawn.health.hediffSet.HasHediff(canSireDef, false))
		{
			pawn.health.GetOrAddHediff(canSireDef);
			if ((removeCarry || (int)pawn.gender == 1) && pawn.health.hediffSet.HasHediff(canCarryDef, false))
			{
				pawn.health.RemoveHediff(pawn.health.GetOrAddHediff(canCarryDef));
			}
		}
	}

	public static bool DecideTransgender(Pawn pawn)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Invalid comparison between Unknown and I4
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Invalid comparison between Unknown and I4
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Invalid comparison between Unknown and I4
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Invalid comparison between Unknown and I4
		bool flag = Rand.Range(0f, 1f) > cisRate;
		if ((int)pawn.gender == 1 && flag)
		{
			SetTrans(pawn);
			if ((int)pawn.gender == 1 && Rand.Range(0f, 1f) <= mCarryRate)
			{
				SetCarry(pawn);
			}
			else if ((int)pawn.gender == 1 && Rand.Range(0f, 1f) > mCarryRate)
			{
				SetSire(pawn);
			}
		}
		else if ((int)pawn.gender == 2 && flag)
		{
			SetTrans(pawn);
			if ((int)pawn.gender == 2 && Rand.Range(0f, 1f) <= fSireRate)
			{
				SetSire(pawn);
			}
			else if ((int)pawn.gender == 2 && Rand.Range(0f, 1f) > fSireRate)
			{
				SetCarry(pawn);
			}
		}
		SimpleTransDebug.Log("Transgender = " + flag, 3);
		return flag;
	}

	public static bool DecideReproductionType(Pawn pawn)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Invalid comparison between Unknown and I4
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Invalid comparison between Unknown and I4
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Invalid comparison between Unknown and I4
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Invalid comparison between Unknown and I4
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Invalid comparison between Unknown and I4
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Invalid comparison between Unknown and I4
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Invalid comparison between Unknown and I4
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Invalid comparison between Unknown and I4
		bool flag = pawn.health.hediffSet.HasHediff(transDef, false);
		bool flag2 = pawn.health.hediffSet.HasHediff(canCarryDef, false);
		bool flag3 = pawn.health.hediffSet.HasHediff(canSireDef, false);
		SimpleTransDebug.Log("Checking reproduction type for " + pawn.Name, 3);
		if ((int)pawn.gender == 1 && flag && Rand.Range(0f, 1f) <= mCarryRate)
		{
			flag2 = true;
			flag3 = false;
		}
		else if ((int)pawn.gender == 1 && flag && Rand.Range(0f, 1f) > mCarryRate)
		{
			flag2 = false;
			flag3 = true;
		}
		else if ((int)pawn.gender == 2 && flag && Rand.Range(0f, 1f) <= fSireRate)
		{
			flag2 = false;
			flag3 = true;
		}
		else if ((int)pawn.gender == 2 && flag && Rand.Range(0f, 1f) > fSireRate)
		{
			flag2 = true;
			flag3 = false;
		}
		else if ((int)pawn.gender == 1 && !flag)
		{
			flag2 = false;
			flag3 = true;
		}
		else if ((int)pawn.gender == 2 && !flag)
		{
			flag2 = true;
			flag3 = false;
		}
		SimpleTransDebug.Log("carry = " + flag2 + " sire = " + flag3, 3);
		if (flag2)
		{
			SetCarry(pawn);
		}
		if (flag3)
		{
			SetSire(pawn);
		}
		return flag2;
	}

	public static void ValidateOrSetGenderWithGenes(Pawn pawn, Gene gene)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Invalid comparison between Unknown and I4
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Invalid comparison between Unknown and I4
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		GeneExtension modExtension = ((Def)gene.def).GetModExtension<GeneExtension>();
		if (modExtension == null || !genesAreAgab)
		{
			return;
		}
		if (modExtension.forceFemale)
		{
			pawn.gender = (Gender)((Rand.Range(0f, 1f) > cisRate) ? 1 : 2);
			if ((int)pawn.gender == 1)
			{
				SetTrans(pawn);
			}
			else if ((int)pawn.gender == 2)
			{
				SetCis(pawn);
			}
			SetCarry(pawn, removeSire: false);
		}
		if (modExtension.forceMale)
		{
			pawn.gender = (Gender)((!(Rand.Range(0f, 1f) > cisRate)) ? 1 : 2);
			if ((int)pawn.gender == 2)
			{
				SetTrans(pawn);
			}
			else
			{
				SetCis(pawn);
			}
			SetSire(pawn, removeCarry: false);
		}
	}

	public static void ClearGender(Pawn pawn)
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
}