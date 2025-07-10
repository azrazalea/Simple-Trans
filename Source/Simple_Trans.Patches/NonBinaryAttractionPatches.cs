using HarmonyLib;
using NonBinaryGender;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace Simple_Trans.Patches
{
    /// <summary>
    /// Shared logic for non-binary attraction across different mods
    /// </summary>
    public static class NonBinaryAttractionUtility
    {
        /// <summary>
        /// Determines if a pawn should be attracted to a target gender based on non-binary rules
        /// Returns true if non-binary attraction applies, false if normal attraction rules should apply
        /// </summary>
        public static bool ShouldBeAttractedToNonBinary(Pawn pawn, Gender targetGender, out bool attractionResult)
        {
            // If target is non-binary (Gender value 3), everyone except aromantic pawns is attracted
            if ((int)targetGender == 3)
            {
                attractionResult = !IsAromantic(pawn);
                return true;
            }

            // If pawn is non-binary, they're attracted to everyone except if they're aromantic
            if (EnbyUtility.IsEnby(pawn))
            {
                attractionResult = !IsAromantic(pawn);
                return true;
            }

            // Not a non-binary case, use normal attraction rules
            attractionResult = false;
            return false;
        }

        /// <summary>
        /// Checks if a pawn is aromantic (uses different methods depending on active mods)
        /// </summary>
        private static bool IsAromantic(Pawn pawn)
        {
            // Check for WayBetterRomance aromantic detection
            if (ModsConfig.IsActive("divineDerivative.Romance"))
            {
                try
                {
                    var sexualityUtilityType = AccessTools.TypeByName("BetterRomance.SexualityUtility");
                    if (sexualityUtilityType != null)
                    {
                        var isAroMethod = AccessTools.Method(sexualityUtilityType, "IsAro");
                        if (isAroMethod != null)
                        {
                            bool result = (bool)isAroMethod.Invoke(null, new object[] { pawn });
                            SimpleTransDebug.Log($"WayBetterRomance IsAro check for {pawn.Name}: {result}", 2);
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    SimpleTransDebug.Log($"Failed to use WayBetterRomance IsAro method: {ex.Message}", 1);
                    // Fall back to vanilla check if WayBetterRomance method fails
                }
            }

            // Vanilla aromantic check - only TraitDefOf.Asexual is truly aromantic
            bool vanillaResult = pawn.story?.traits?.HasTrait(TraitDefOf.Asexual) == true;
            SimpleTransDebug.Log($"Vanilla aromantic check for {pawn.Name}: {vanillaResult}", 2);
            return vanillaResult;
        }
    }

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
            bool nonBinaryActive = ModsConfig.IsActive("divineDerivative.NonBinaryGender");

            // Don't apply if WayBetterRomance is active - use WayBetterRomance patch instead
            bool wayBetterRomanceActive = ModsConfig.IsActive("divineDerivative.Romance");

            return nonBinaryActive && !wayBetterRomanceActive;
        }

        /// <summary>
        /// Prefix patch that handles non-binary attraction for vanilla RelationsUtility
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, Gender gender, ref bool __result)
        {
            if (NonBinaryAttractionUtility.ShouldBeAttractedToNonBinary(pawn, gender, out bool attractionResult))
            {
                SimpleTransDebug.Log($"Vanilla patch: {pawn.Name} attraction to gender {(int)gender}: {attractionResult}", 2);
                __result = attractionResult;
                return false; // Skip original method
            }

            // For other genders, continue with original method
            return true;
        }
    }

    /// <summary>
    /// Harmony patch for WayBetterRomance's custom AttractedToGender logic
    /// Only applied when both NonBinary Gender mod and WayBetterRomance are loaded
    /// </summary>
    [HarmonyPatch]
    public static class WayBetterRomanceNonBinaryAttractionPatch
    {
        /// <summary>
        /// Dynamically targets WayBetterRomance's AttractedToGender patch
        /// </summary>
        public static MethodBase TargetMethod()
        {
            Type relationsUtilityPatchType = AccessTools.TypeByName("BetterRomance.RelationsUtility_AttractedToGender");
            if (relationsUtilityPatchType != null)
            {
                var method = AccessTools.Method(relationsUtilityPatchType, "Prefix");
                if (method != null)
                {
                    SimpleTransDebug.Log($"Successfully found target method: BetterRomance.RelationsUtility_AttractedToGender.Prefix", 1);
                    return method;
                }
                else
                {
                    SimpleTransDebug.Log($"Failed to find method Prefix in BetterRomance.RelationsUtility_AttractedToGender", 1);
                }
            }
            else
            {
                SimpleTransDebug.Log($"Failed to find type BetterRomance.RelationsUtility_AttractedToGender", 1);
            }
            return null;
        }

        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            bool nonBinaryActive = ModsConfig.IsActive("divineDerivative.NonBinaryGender");
            bool wayBetterRomanceActive = ModsConfig.IsActive("divineDerivative.Romance");

            return nonBinaryActive && wayBetterRomanceActive && TargetMethod() != null;
        }

        /// <summary>
        /// Prefix patch that handles non-binary attraction before WayBetterRomance's logic
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, Gender gender, ref bool __result)
        {
            if (NonBinaryAttractionUtility.ShouldBeAttractedToNonBinary(pawn, gender, out bool attractionResult))
            {
                SimpleTransDebug.Log($"WayBetterRomance patch: {pawn.Name} attraction to gender {(int)gender}: {attractionResult}", 2);
                __result = attractionResult;
                return false; // Skip WayBetterRomance's method
            }

            // For other genders, let WayBetterRomance handle it
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
            bool nonBinaryActive = ModsConfig.IsActive("divineDerivative.NonBinaryGender");
            bool intimacyActive = ModsConfig.IsActive("LoveyDoveySexWithEuterpe");

            return nonBinaryActive && intimacyActive && TargetMethod() != null;
        }

        /// <summary>
        /// Prefix patch that handles non-binary attraction for Intimacy mod
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, Gender gender, ref bool __result)
        {
            if (NonBinaryAttractionUtility.ShouldBeAttractedToNonBinary(pawn, gender, out bool attractionResult))
            {
                SimpleTransDebug.Log($"Intimacy patch: {pawn.Name} attraction to gender {(int)gender}: {attractionResult}", 2);
                __result = attractionResult;
                return false; // Skip original method
            }

            // For other genders, continue with original method
            return true;
        }
    }
}
