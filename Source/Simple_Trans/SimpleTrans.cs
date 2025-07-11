using HarmonyLib;
using Simple_Trans.Patches;
using Verse;
using XmlExtensions;

namespace Simple_Trans;

/// <summary>
/// Main mod class for Simple Trans
/// Handles initialization, mod detection, and Harmony patching
/// </summary>
[StaticConstructorOnStartup]
public static class SimpleTrans
{
	#region Mod Detection Properties

	/// <summary>
	/// True if Non-Binary Gender mod is active
	/// </summary>
	public static bool NBGenderActive { get; private set; }

	/// <summary>
	/// True if Humanoid Alien Races mod is active
	/// </summary>
	public static bool HARActive { get; private set; }

	/// <summary>
	/// True if Ideology DLC is active
	/// </summary>
	public static bool IdeologyActive { get; private set; }

	/// <summary>
	/// True if debug mode is enabled in mod settings
	/// </summary>
	public static bool debugMode { get; set; }

	#endregion

	#region Initialization

	/// <summary>
	/// Static constructor - performs mod initialization and Harmony patching
	/// </summary>
	static SimpleTrans()
	{
		try
		{
			Harmony harmony = new Harmony("runaway.simple_trans");
			harmony.PatchAll();

			DetectActiveMods();
			LoadDebugMode();

			if (NBGenderActive)
			{
				harmony.PatchNBG();
			}

			if (IdeologyActive)
			{
				TryPatchBiosculpter(harmony);
				TryInitializeRitualSystem();
			}

			PregnancyApplicationPatches.TryPatchAllPregnancySystems(harmony);

			SimpleTransPregnancyUtility.LoadSettings();
			Log.Message("[Simple Trans] Mod initialized successfully");
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Critical error during mod initialization: {ex}");
		}
	}

	/// <summary>
	/// Detects compatible mod dependencies
	/// </summary>
	private static void DetectActiveMods()
	{
		try
		{
			HARActive = ModsConfig.IsActive("erdelf.humanoidalienraces") || ModsConfig.IsActive("erdelf.humanoidalienraces.dev");
			NBGenderActive = ModsConfig.IsActive("divinederivative.nonbinarygender");
			IdeologyActive = ModsConfig.IdeologyActive;

			if (debugMode)
			{
				Log.Message($"[Simple Trans] Mod detection - HAR: {HARActive}, NBG: {NBGenderActive}, Ideology: {IdeologyActive}");
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error detecting active mods: {ex}");
			// Set safe defaults on error
			HARActive = false;
			NBGenderActive = false;
			IdeologyActive = false;
		}
	}

	/// <summary>
	/// Loads debug mode setting from XML Extensions
	/// </summary>
	private static void LoadDebugMode()
	{
		try
		{
			string debugSetting = SettingsManager.GetSetting("runaway.simpletrans", "debugMode");

			if (string.IsNullOrEmpty(debugSetting))
			{
				debugMode = false;
			}
			else if (bool.TryParse(debugSetting, out bool result))
			{
				debugMode = result;
			}
			else
			{
				Log.Warning($"[Simple Trans] Invalid debug mode setting '{debugSetting}', defaulting to false");
				debugMode = false;
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error loading debug mode setting: {ex}");
			debugMode = false;
		}
	}

	/// <summary>
	/// Conditionally applies biosculpter pod patches when Ideology DLC is active
	/// </summary>
	private static void TryPatchBiosculpter(Harmony harmony)
	{
		try
		{
			// Manual patch required since [HarmonyPatch] attribute conflicts with conditional loading
			var originalMethod = AccessTools.Method(typeof(RimWorld.CompBiosculpterPod), "OrderToPod");
			var prefixMethod = AccessTools.Method(typeof(SimpleTransBiosculpterPatch), "OrderToPod_Prefix");

			if (originalMethod != null && prefixMethod != null)
			{
				harmony.Patch(originalMethod, prefix: new HarmonyMethod(prefixMethod));

				if (debugMode)
				{
					Log.Message("[Simple Trans] Successfully patched biosculpter pod system");
				}
			}
			else
			{
				Log.Error("[Simple Trans] Failed to find biosculpter patch methods - CompBiosculpterPod.OrderToPod or OrderToPod_Prefix not found");
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error patching biosculpter system: {ex}");
		}
	}

	/// <summary>
	/// Conditionally initializes ritual system when Ideology DLC is active
	/// </summary>
	private static void TryInitializeRitualSystem()
	{
		try
		{
			// The ritual system classes (RitualBehaviorWorker_GenderAffirmParty, RitualOutcomeEffectWorker_GenderAffirmParty)
			// are automatically registered when the mod loads, and the XML definitions are conditionally loaded
			// via PatchOperationFindMod. No additional initialization is required.

			if (debugMode)
			{
				Log.Message("[Simple Trans] Ritual system initialized successfully");
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error initializing ritual system: {ex}");
		}
	}

	#endregion
}
