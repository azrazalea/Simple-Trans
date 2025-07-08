using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace Simple_Trans.Patches
{
    /// <summary>
    /// Harmony patches for non-binary gender attraction compatibility
    /// Only applied when NonBinary Gender mod is loaded
    /// </summary>
    [HarmonyPatch(typeof(RelationsUtility), "AttractedToGender")]
    public static class NonBinaryAttractionPatch
    {
        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            return ModsConfig.IsActive("Coraizon.NonBinaryGenderMod") ||
                   ModsConfig.IsActive("Coraizon.NBGM");
        }

        /// <summary>
        /// Prefix patch that makes all pawns consider non-binary gender (value 3) as attractive
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, Gender gender, ref bool __result)
        {
            // If the target gender is non-binary (Gender value 3), make everyone attracted
            if ((int)gender == 3)
            {
                __result = true;
                return false; // Skip original method
            }

            // For other genders, continue with original method
            return true;
        }
    }

    /// <summary>
    /// Harmony patch for Intimacy mod's CommonChecks.AttractedToGender
    /// Only applied when both NonBinary Gender mod and Intimacy mod are loaded
    /// </summary>
    [HarmonyPatch]
    public static class IntimacyNonBinaryAttractionPatch
    {
        /// <summary>
        /// Dynamically targets CommonChecks.AttractedToGender from Intimacy mod
        /// </summary>
        public static MethodBase TargetMethod()
        {
            // Look for the Intimacy mod's CommonChecks type
            Type commonChecksType = AccessTools.TypeByName("LoveyDoveySexWithEuterpe.CommonChecks");
            if (commonChecksType != null)
            {
                return AccessTools.Method(commonChecksType, "AttractedToGender");
            }
            return null;
        }

        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            // Check if both NonBinary Gender mod AND Intimacy mod are active
            bool nonBinaryActive = ModsConfig.IsActive("Coraizon.NonBinaryGenderMod") ||
                                  ModsConfig.IsActive("Coraizon.NBGM");
            bool intimacyActive = ModsConfig.IsActive("LoveyDoveySexWithEuterpe");

            return nonBinaryActive && intimacyActive && TargetMethod() != null;
        }

        /// <summary>
        /// Prefix patch that makes all pawns consider non-binary gender (value 3) as attractive
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, Gender gender, ref bool __result)
        {
            // If the target gender is non-binary (Gender value 3), make everyone attracted
            if ((int)gender == 3)
            {
                __result = true;
                return false; // Skip original method
            }

            // For other genders, continue with original method
            return true;
        }
    }
}
