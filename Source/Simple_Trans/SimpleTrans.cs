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
	/// True if debug mode is enabled in mod settings
	/// </summary>
	public static bool debugMode { get; private set; }
	
	#endregion

	#region Initialization
	
	/// <summary>
	/// Static constructor - called automatically when the class is first accessed
	/// Performs mod initialization and Harmony patching
	/// </summary>
	static SimpleTrans()
	{
		try
		{
			// Initialize Harmony and apply patches
			Harmony harmony = new Harmony("runaway.simple_trans");
			harmony.PatchAll();
			
			// Detect active mods
			DetectActiveMods();
			
			// Load debug mode setting
			LoadDebugMode();
			
			// Apply Non-Binary Gender patches if mod is active
			if (NBGenderActive)
			{
				harmony.PatchNBG();
			}
			
			Log.Message("[Simple Trans] Mod initialized successfully");
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Critical error during mod initialization: {ex}");
		}
	}
	
	/// <summary>
	/// Detects which compatible mods are currently active
	/// </summary>
	private static void DetectActiveMods()
	{
		try
		{
			HARActive = ModsConfig.IsActive("erdelf.humanoidalienraces") || ModsConfig.IsActive("erdelf.humanoidalienraces.dev");
			NBGenderActive = ModsConfig.IsActive("divinederivative.nonbinarygender");
			
			if (debugMode)
			{
				Log.Message($"[Simple Trans] Mod detection - HAR: {HARActive}, NBG: {NBGenderActive}");
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error detecting active mods: {ex}");
			// Set safe defaults
			HARActive = false;
			NBGenderActive = false;
		}
	}
	
	/// <summary>
	/// Loads the debug mode setting from mod configuration
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
	
	#endregion
}
