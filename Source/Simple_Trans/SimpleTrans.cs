using HarmonyLib;
using Simple_Trans.Patches;
using Verse;
using XmlExtensions;

namespace Simple_Trans;

[StaticConstructorOnStartup]
public static class SimpleTrans
{
	public static bool NBGenderActive;

	public static bool HARActive;

	public static bool debugMode;

	static SimpleTrans()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		Harmony val = new Harmony("runaway.simple_trans");
		val.PatchAll();
		HARActive = ModsConfig.IsActive("erdelf.humanoidalienraces") || ModsConfig.IsActive("erdelf.humanoidalienraces.dev");
		NBGenderActive = ModsConfig.IsActive("divinederivative.nonbinarygender");
		debugMode = bool.Parse(SettingsManager.GetSetting("runaway.simpletrans", "debugMode"));
		if (NBGenderActive)
		{
			val.PatchNBG();
		}
	}
}
