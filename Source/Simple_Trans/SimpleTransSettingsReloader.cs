using XmlExtensions.Action;
using Verse;

namespace Simple_Trans;

/// <summary>
/// ActionContainer that reloads mod settings when the settings window is closed
/// This enables live settings reloading without requiring a game restart
/// </summary>
public class SimpleTransSettingsReloader : ActionContainer
{
	/// <summary>
	/// Called when the settings window is closed
	/// Reloads all mod settings and debug mode
	/// </summary>
	protected override bool ApplyAction()
	{
		try
		{
			// Reload all mod settings
			SimpleTransSettings.LoadSettings();
			
			// Reload debug mode setting
			ReloadDebugMode();
			
			SimpleTransDebug.Log("Settings reloaded successfully after settings window closed", 2);
			
			return true;
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error reloading settings: {ex}");
			return false;
		}
	}
	
	/// <summary>
	/// Reloads the debug mode setting (mirrors SimpleTrans.LoadDebugMode)
	/// </summary>
	private void ReloadDebugMode()
	{
		try
		{
			string debugSetting = XmlExtensions.SettingsManager.GetSetting("runaway.simpletrans", "debugMode");

			if (string.IsNullOrEmpty(debugSetting))
			{
				SimpleTrans.DebugMode = false;
			}
			else if (bool.TryParse(debugSetting, out bool result))
			{
				SimpleTrans.DebugMode = result;
			}
			else
			{
				Log.Warning($"[Simple Trans] Invalid debug mode setting '{debugSetting}', defaulting to false");
				SimpleTrans.DebugMode = false;
			}
		}
		catch (System.Exception ex)
		{
			Log.Error($"[Simple Trans] Error reloading debug mode setting: {ex}");
			SimpleTrans.DebugMode = false;
		}
	}
}