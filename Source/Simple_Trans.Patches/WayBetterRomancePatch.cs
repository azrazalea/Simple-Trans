using HarmonyLib;
using RimWorld;
using Verse;

namespace Simple_Trans.Patches
{
    /// <summary>
    /// Patches for WayBetterRomance compatibility with nonbinary gender
    /// </summary>
    [HarmonyPatch]
    public static class WayBetterRomanceMinAgeToHaveChildren_Patch
    {
        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            bool wayBetterRomanceActive = ModsConfig.IsActive("divineDerivative.Romance");
            bool nonBinaryActive = ModsConfig.IsActive("divineDerivative.NonBinaryGender");

            return wayBetterRomanceActive && nonBinaryActive;
        }

        /// <summary>
        /// Dynamically targets BetterRomance.SettingsUtilities.MinAgeToHaveChildren
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            var settingsUtilityType = AccessTools.TypeByName("BetterRomance.SettingsUtilities");
            if (settingsUtilityType != null)
            {
                var method = AccessTools.Method(settingsUtilityType, "MinAgeToHaveChildren");
                if (method != null)
                {
                    SimpleTransDebug.Log($"Successfully found target method: BetterRomance.SettingsUtilities.MinAgeToHaveChildren", 1);
                    return method;
                }
                else
                {
                    SimpleTransDebug.Log($"Failed to find method MinAgeToHaveChildren in BetterRomance.SettingsUtilities", 1);
                }
            }
            else
            {
                SimpleTransDebug.Log($"Failed to find type BetterRomance.SettingsUtilities", 1);
            }
            return null;
        }

        /// <summary>
        /// Prefix patch that handles Gender.Enby before the method throws an exception
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, Gender gender, ref float __result)
        {
            // Handle Gender.None (value 0) - WayBetterRomance bug
            if ((int)gender == 0)
            {
                Gender actualGender = pawn?.gender ?? Gender.Female;
                if ((int)actualGender == 0 || (int)actualGender == 3)
                {
                    __result = 16f; // Default minimum age for females
                    return false;
                }
                return true; // Continue to original method
            }

            // Handle Gender.Enby (value 3)
            if ((int)gender == 3)
            {
                try
                {
                    var settingsUtilityType = AccessTools.TypeByName("BetterRomance.SettingsUtilities");
                    var getRelationSettingsMethod = AccessTools.Method(settingsUtilityType, "GetRelationSettings");
                    if (getRelationSettingsMethod != null)
                    {
                        var settings = getRelationSettingsMethod.Invoke(null, new object[] { pawn });
                        if (settings != null)
                        {
                            var minFemaleAgeField = AccessTools.Field(settings.GetType(), "minFemaleAgeToHaveChildren");
                            if (minFemaleAgeField != null)
                            {
                                __result = (float)minFemaleAgeField.GetValue(settings);
                                return false;
                            }
                        }
                    }
                }
                catch
                {
                    // Silent fallback
                }

                __result = 16f; // Default minimum age
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Patches for WayBetterRomance MaxAgeToHaveChildren compatibility with nonbinary gender
    /// </summary>
    [HarmonyPatch]
    public static class WayBetterRomanceMaxAgeToHaveChildren_Patch
    {
        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            bool wayBetterRomanceActive = ModsConfig.IsActive("divineDerivative.Romance");
            bool nonBinaryActive = ModsConfig.IsActive("divineDerivative.NonBinaryGender");

            return wayBetterRomanceActive && nonBinaryActive;
        }

        /// <summary>
        /// Dynamically targets BetterRomance.SettingsUtilities.MaxAgeToHaveChildren
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            var settingsUtilityType = AccessTools.TypeByName("BetterRomance.SettingsUtilities");
            if (settingsUtilityType != null)
            {
                var method = AccessTools.Method(settingsUtilityType, "MaxAgeToHaveChildren");
                if (method != null)
                {
                    SimpleTransDebug.Log($"Successfully found target method: BetterRomance.SettingsUtilities.MaxAgeToHaveChildren", 1);
                    return method;
                }
                else
                {
                    SimpleTransDebug.Log($"Failed to find method MaxAgeToHaveChildren in BetterRomance.SettingsUtilities", 1);
                }
            }
            else
            {
                SimpleTransDebug.Log($"Failed to find type BetterRomance.SettingsUtilities", 1);
            }
            return null;
        }

        /// <summary>
        /// Prefix patch that handles Gender.Enby before the method throws an exception
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, Gender gender, ref float __result)
        {
            // Handle Gender.None (value 0) - WayBetterRomance bug
            if ((int)gender == 0)
            {
                Gender actualGender = pawn?.gender ?? Gender.Female;
                if ((int)actualGender == 0 || (int)actualGender == 3)
                {
                    __result = 50f; // Default maximum age for females
                    return false;
                }
                return true; // Continue to original method
            }

            // Handle Gender.Enby (value 3)
            if ((int)gender == 3)
            {
                try
                {
                    var settingsUtilityType = AccessTools.TypeByName("BetterRomance.SettingsUtilities");
                    var getRelationSettingsMethod = AccessTools.Method(settingsUtilityType, "GetRelationSettings");
                    if (getRelationSettingsMethod != null)
                    {
                        var settings = getRelationSettingsMethod.Invoke(null, new object[] { pawn });
                        if (settings != null)
                        {
                            var maxFemaleAgeField = AccessTools.Field(settings.GetType(), "maxFemaleAgeToHaveChildren");
                            if (maxFemaleAgeField != null)
                            {
                                __result = (float)maxFemaleAgeField.GetValue(settings);
                                return false;
                            }
                        }
                    }
                }
                catch
                {
                    // Silent fallback
                }

                __result = 50f; // Default maximum age
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Patches for WayBetterRomance UsualAgeToHaveChildren compatibility with nonbinary gender
    /// </summary>
    [HarmonyPatch]
    public static class WayBetterRomanceUsualAgeToHaveChildren_Patch
    {
        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            bool wayBetterRomanceActive = ModsConfig.IsActive("divineDerivative.Romance");
            bool nonBinaryActive = ModsConfig.IsActive("divineDerivative.NonBinaryGender");

            return wayBetterRomanceActive && nonBinaryActive;
        }

        /// <summary>
        /// Dynamically targets BetterRomance.SettingsUtilities.UsualAgeToHaveChildren
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            var settingsUtilityType = AccessTools.TypeByName("BetterRomance.SettingsUtilities");
            if (settingsUtilityType != null)
            {
                var method = AccessTools.Method(settingsUtilityType, "UsualAgeToHaveChildren");
                if (method != null)
                {
                    SimpleTransDebug.Log($"Successfully found target method: BetterRomance.SettingsUtilities.UsualAgeToHaveChildren", 1);
                    return method;
                }
                else
                {
                    SimpleTransDebug.Log($"Failed to find method UsualAgeToHaveChildren in BetterRomance.SettingsUtilities", 1);
                }
            }
            else
            {
                SimpleTransDebug.Log($"Failed to find type BetterRomance.SettingsUtilities", 1);
            }
            return null;
        }

        /// <summary>
        /// Prefix patch that handles Gender.Enby before the method throws an exception
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, Gender gender, ref float __result)
        {
            // Handle Gender.None (value 0) - WayBetterRomance bug
            if ((int)gender == 0)
            {
                Gender actualGender = pawn?.gender ?? Gender.Female;
                if ((int)actualGender == 0 || (int)actualGender == 3)
                {
                    __result = 28f; // Default usual age for females
                    return false;
                }
                return true; // Continue to original method
            }

            // Handle Gender.Enby (value 3)
            if ((int)gender == 3)
            {
                try
                {
                    var settingsUtilityType = AccessTools.TypeByName("BetterRomance.SettingsUtilities");
                    var getRelationSettingsMethod = AccessTools.Method(settingsUtilityType, "GetRelationSettings");
                    if (getRelationSettingsMethod != null)
                    {
                        var settings = getRelationSettingsMethod.Invoke(null, new object[] { pawn });
                        if (settings != null)
                        {
                            var usualFemaleAgeField = AccessTools.Field(settings.GetType(), "usualFemaleAgeToHaveChildren");
                            if (usualFemaleAgeField != null)
                            {
                                __result = (float)usualFemaleAgeField.GetValue(settings);
                                return false;
                            }
                        }
                    }
                }
                catch
                {
                    // Silent fallback
                }

                __result = 28f; // Default usual age for females
                return false;
            }

            return true;
        }
    }
}
