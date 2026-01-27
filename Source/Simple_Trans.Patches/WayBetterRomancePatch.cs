using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;

namespace Simple_Trans.Patches
{
    /// <summary>
    /// Shared constants and utilities for WayBetterRomance patches
    /// </summary>
    public static class WayBetterRomancePatchConstants
    {
        /// <summary>
        /// Gender.Enby enum value (3) - used for non-binary gender detection
        /// </summary>
        public const int GENDER_ENBY_VALUE = 3;

        /// <summary>
        /// Default minimum age for females (used as fallback for non-binary)
        /// </summary>
        public const float DEFAULT_MIN_AGE = 16f;

        /// <summary>
        /// Default maximum age for females (used as fallback for non-binary)  
        /// </summary>
        public const float DEFAULT_MAX_AGE = 50f;

        /// <summary>
        /// Default usual age for females (used as fallback for non-binary)
        /// </summary>
        public const float DEFAULT_USUAL_AGE = 28f;
    }

    /// <summary>
    /// Shared utilities for WayBetterRomance patches
    /// </summary>
    public static class WayBetterRomancePatchUtilities
    {
        /// <summary>
        /// Standard prepare logic for WayBetterRomance + NonBinary Gender patches
        /// </summary>
        public static bool StandardPrepare()
        {
            // Use cached values from SimpleTrans initialization
            return SimpleTrans.WBRActive && SimpleTrans.NBGenderActive;
        }

        /// <summary>
        /// Prepare logic for WayBetterRomance patches that work with Simple Trans only
        /// </summary>
        public static bool WayBetterRomancePrepare()
        {
            // Use cached value from SimpleTrans initialization  
            return SimpleTrans.WBRActive;
        }

        /// <summary>
        /// Gets a WayBetterRomance SettingsUtilities method by name
        /// </summary>
        public static System.Reflection.MethodBase GetSettingsUtilitiesMethod(string methodName)
        {
            var settingsUtilityType = AccessTools.TypeByName("BetterRomance.SettingsUtilities");
            if (settingsUtilityType != null)
            {
                var method = AccessTools.Method(settingsUtilityType, methodName);
                if (method != null)
                {
                    SimpleTransDebug.Log($"Successfully found target method: BetterRomance.SettingsUtilities.{methodName}", 1);
                    return method;
                }
                else
                {
                    SimpleTransDebug.Log($"Failed to find method {methodName} in BetterRomance.SettingsUtilities", 1);
                }
            }
            else
            {
                SimpleTransDebug.Log($"Failed to find type BetterRomance.SettingsUtilities", 1);
            }
            return null;
        }

        /// <summary>
        /// Handles Gender.None edge case that appears in WayBetterRomance
        /// </summary>
        public static bool HandleGenderNone(Gender gender, Pawn pawn, float defaultValue, ref float __result)
        {
            if ((int)gender == 0) // Gender.None
            {
                Gender actualGender = pawn?.gender ?? Gender.Female;
                if ((int)actualGender == 0 || (int)actualGender == WayBetterRomancePatchConstants.GENDER_ENBY_VALUE)
                {
                    __result = defaultValue;
                    return true; // Handled
                }
                return false; // Let original method handle this case
            }
            return false; // Not Gender.None
        }

        /// <summary>
        /// Tries to get a WayBetterRomance settings field value
        /// </summary>
        public static bool TryGetSettingsFieldValue(Pawn pawn, string fieldName, out float value)
        {
            value = 0f;
            try
            {
                var settingsUtilityType = AccessTools.TypeByName("BetterRomance.SettingsUtilities");
                var getRelationSettingsMethod = AccessTools.Method(settingsUtilityType, "GetRelationSettings");
                if (getRelationSettingsMethod != null)
                {
                    var settings = getRelationSettingsMethod.Invoke(null, new object[] { pawn });
                    if (settings != null)
                    {
                        var field = AccessTools.Field(settings.GetType(), fieldName);
                        if (field != null)
                        {
                            value = (float)field.GetValue(settings);
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Silent fallback
            }
            return false;
        }

        /// <summary>
        /// Gets a WayBetterRomance utility method by type and method name
        /// </summary>
        public static System.Reflection.MethodBase GetUtilityMethod(string typeName, string methodName)
        {
            var utilityType = AccessTools.TypeByName(typeName);
            if (utilityType != null)
            {
                var method = AccessTools.Method(utilityType, methodName);
                if (method != null)
                {
                    SimpleTransDebug.Log($"Successfully found target method: {typeName}.{methodName}", 1);
                    return method;
                }
                else
                {
                    SimpleTransDebug.Log($"Failed to find method {methodName} in {typeName}", 1);
                }
            }
            else
            {
                SimpleTransDebug.Log($"Failed to find type {typeName}", 1);
            }
            return null;
        }
    }

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
            return WayBetterRomancePatchUtilities.WayBetterRomancePrepare();
        }

        /// <summary>
        /// Dynamically targets BetterRomance.SettingsUtilities.MinAgeToHaveChildren
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            return WayBetterRomancePatchUtilities.GetSettingsUtilitiesMethod("MinAgeToHaveChildren");
        }

        /// <summary>
        /// Prefix patch that handles Gender.Enby before the method throws an exception
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Prefix(Pawn pawn, Gender gender, ref float __result)
        {
            // Handle Gender.None (value 0) - WayBetterRomance bug
            if (WayBetterRomancePatchUtilities.HandleGenderNone(gender, pawn, WayBetterRomancePatchConstants.DEFAULT_MIN_AGE, ref __result))
            {
                return false; // Handled by utility
            }
            if ((int)gender == 0)
            {
                return true; // Continue to original method for other Gender.None cases
            }

            // Handle Gender.Enby (value 3)
            if ((int)gender == WayBetterRomancePatchConstants.GENDER_ENBY_VALUE)
            {
                // Try to get WayBetterRomance setting, fallback to default
                if (WayBetterRomancePatchUtilities.TryGetSettingsFieldValue(pawn, "minFemaleAgeToHaveChildren", out float settingsValue))
                {
                    __result = settingsValue;
                }
                else
                {
                    __result = WayBetterRomancePatchConstants.DEFAULT_MIN_AGE; // Default minimum age
                }
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
            return WayBetterRomancePatchUtilities.WayBetterRomancePrepare();
        }

        /// <summary>
        /// Dynamically targets BetterRomance.SettingsUtilities.MaxAgeToHaveChildren
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            return WayBetterRomancePatchUtilities.GetSettingsUtilitiesMethod("MaxAgeToHaveChildren");
        }

        /// <summary>
        /// Prefix patch that handles Gender.Enby before the method throws an exception
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Prefix(Pawn pawn, Gender gender, ref float __result)
        {
            // Handle Gender.None (value 0) - WayBetterRomance bug
            if (WayBetterRomancePatchUtilities.HandleGenderNone(gender, pawn, WayBetterRomancePatchConstants.DEFAULT_MAX_AGE, ref __result))
            {
                return false; // Handled by utility
            }
            if ((int)gender == 0)
            {
                return true; // Continue to original method for other Gender.None cases
            }

            // Handle Gender.Enby (value 3)
            if ((int)gender == WayBetterRomancePatchConstants.GENDER_ENBY_VALUE)
            {
                // Try to get WayBetterRomance setting, fallback to default
                if (WayBetterRomancePatchUtilities.TryGetSettingsFieldValue(pawn, "maxFemaleAgeToHaveChildren", out float settingsValue))
                {
                    __result = settingsValue;
                }
                else
                {
                    __result = WayBetterRomancePatchConstants.DEFAULT_MAX_AGE; // Default maximum age
                }
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
            return WayBetterRomancePatchUtilities.WayBetterRomancePrepare();
        }

        /// <summary>
        /// Dynamically targets BetterRomance.SettingsUtilities.UsualAgeToHaveChildren
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            return WayBetterRomancePatchUtilities.GetSettingsUtilitiesMethod("UsualAgeToHaveChildren");
        }

        /// <summary>
        /// Prefix patch that handles Gender.Enby before the method throws an exception
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Prefix(Pawn pawn, Gender gender, ref float __result)
        {
            // Handle Gender.None (value 0) - WayBetterRomance bug
            if (WayBetterRomancePatchUtilities.HandleGenderNone(gender, pawn, WayBetterRomancePatchConstants.DEFAULT_USUAL_AGE, ref __result))
            {
                return false; // Handled by utility
            }
            if ((int)gender == 0)
            {
                return true; // Continue to original method for other Gender.None cases
            }

            // Handle Gender.Enby (value 3)
            if ((int)gender == WayBetterRomancePatchConstants.GENDER_ENBY_VALUE)
            {
                // Try to get WayBetterRomance setting, fallback to default
                if (WayBetterRomancePatchUtilities.TryGetSettingsFieldValue(pawn, "usualFemaleAgeToHaveChildren", out float settingsValue))
                {
                    __result = settingsValue;
                }
                else
                {
                    __result = WayBetterRomancePatchConstants.DEFAULT_USUAL_AGE; // Default usual age
                }
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Patches for WayBetterRomance PreceptUtility.AllowedSpouseCount compatibility with nonbinary gender
    /// </summary>
    [HarmonyPatch]
    public static class WayBetterRomanceAllowedSpouseCountPatch
    {
        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            return WayBetterRomancePatchUtilities.WayBetterRomancePrepare();
        }

        /// <summary>
        /// Dynamically targets BetterRomance.PreceptUtility.AllowedSpouseCount
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            return WayBetterRomancePatchUtilities.GetUtilityMethod("BetterRomance.PreceptUtility", "AllowedSpouseCount");
        }

        /// <summary>
        /// Prefix patch that handles Gender.Enby before the method throws an exception
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Prefix(Ideo ideo, Gender gender, ref int __result)
        {
            // Handle Gender.Enby (value 3) - treat as Female for spouse count purposes
            if ((int)gender == WayBetterRomancePatchConstants.GENDER_ENBY_VALUE)
            {
                try
                {
                    var preceptUtilityType = AccessTools.TypeByName("BetterRomance.PreceptUtility");
                    var method = AccessTools.Method(preceptUtilityType, "AllowedSpouseCount");
                    if (method != null)
                    {
                        // Call the original method with Female gender as fallback
                        __result = (int)method.Invoke(null, new object[] { ideo, Gender.Female });
                        SimpleTransDebug.Log($"WayBetterRomance AllowedSpouseCount patch: Using Female fallback for non-binary gender, result: {__result}", 2);
                        return false;
                    }
                }
                catch (System.Exception ex)
                {
                    SimpleTransDebug.Log($"Error in WayBetterRomance AllowedSpouseCount patch: {ex.Message}", 1);
                }

                // Fallback to reasonable default
                __result = 1; // Default to monogamous
                SimpleTransDebug.Log($"WayBetterRomance AllowedSpouseCount patch: Using fallback default for non-binary gender, result: {__result}", 2);
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Patches for WayBetterRomance HookupUtility.CanEverProduceChild compatibility with nonbinary gender
    /// </summary>
    [HarmonyPatch]
    public static class WayBetterRomanceCanEverProduceChildPatch
    {
        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            return WayBetterRomancePatchUtilities.WayBetterRomancePrepare();
        }

        /// <summary>
        /// Dynamically targets BetterRomance.HookupUtility.CanEverProduceChild
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            return WayBetterRomancePatchUtilities.GetUtilityMethod("BetterRomance.HookupUtility", "CanEverProduceChild");
        }

        /// <summary>
        /// Prefix patch that handles non-binary gender for pregnancy compatibility
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Prefix(Pawn first, Pawn second, ref AcceptanceReport __result)
        {
            // If either pawn is non-binary, use Simple Trans capability-based logic
            if (SimpleTransHediffs.IsEnby(first) || SimpleTransHediffs.IsEnby(second))
            {
                // Check if either pawn can carry and either can sire
                bool canCarry = SimpleTransHediffs.CanCarry(first) || SimpleTransHediffs.CanCarry(second);
                bool canSire = SimpleTransHediffs.CanSire(first) || SimpleTransHediffs.CanSire(second);

                if (canCarry && canSire)
                {
                    __result = AcceptanceReport.WasAccepted; // No issue, pregnancy is possible
                    SimpleTransDebug.Log($"WayBetterRomance CanEverProduceChild patch: Non-binary pawns {first.Name} and {second.Name} can produce children", 2);
                }
                else
                {
                    __result = new AcceptanceReport("WBR.PawnsCannotProduceChildren".Translate(first.Named("PAWN1"), second.Named("PAWN2")));
                    SimpleTransDebug.Log($"WayBetterRomance CanEverProduceChild patch: Non-binary pawns {first.Name} and {second.Name} cannot produce children", 2);
                }
                return false; // Skip original method
            }

            return true; // Continue with original method for binary genders
        }
    }

    /// <summary>
    /// Patches for WayBetterRomance RomanceUtilities.GetAppropriateParentRelationship compatibility with nonbinary gender
    /// </summary>
    [HarmonyPatch]
    public static class WayBetterRomanceGetAppropriateParentRelationshipPatch
    {
        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            return WayBetterRomancePatchUtilities.WayBetterRomancePrepare();
        }

        /// <summary>
        /// Dynamically targets BetterRomance.RomanceUtilities.GetAppropriateParentRelationship
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            return WayBetterRomancePatchUtilities.GetUtilityMethod("BetterRomance.RomanceUtilities", "GetAppropriateParentRelationship");
        }

        /// <summary>
        /// Prefix patch that handles non-binary gender for parent relationship assignment
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Prefix(Pawn first, Pawn second, ref PawnRelationDef __result)
        {
            // If either pawn is non-binary, use capability-based parent assignment
            if (SimpleTransHediffs.IsEnby(first) || SimpleTransHediffs.IsEnby(second))
            {
                // Determine who can carry (mother) and who can sire (father)
                bool firstCanCarry = SimpleTransHediffs.CanCarry(first);
                bool secondCanCarry = SimpleTransHediffs.CanCarry(second);
                bool firstCanSire = SimpleTransHediffs.CanSire(first);
                bool secondCanSire = SimpleTransHediffs.CanSire(second);

                // Assign parent relationships based on capabilities
                if (firstCanCarry && secondCanSire)
                {
                    __result = PawnRelationDefOf.Child; // Standard parent-child relationship
                    SimpleTransDebug.Log($"WayBetterRomance GetAppropriateParentRelationship patch: {first.Name} (carrier) and {second.Name} (sire)", 2);
                }
                else if (secondCanCarry && firstCanSire)
                {
                    __result = PawnRelationDefOf.Child; // Standard parent-child relationship
                    SimpleTransDebug.Log($"WayBetterRomance GetAppropriateParentRelationship patch: {second.Name} (carrier) and {first.Name} (sire)", 2);
                }
                else
                {
                    // Default to standard parent relationships if capabilities are unclear
                    __result = PawnRelationDefOf.Child;
                    SimpleTransDebug.Log($"WayBetterRomance GetAppropriateParentRelationship patch: Default assignment for {first.Name} and {second.Name}", 2);
                }
                return false; // Skip original method
            }

            return true; // Continue with original method for binary genders
        }
    }

    /// <summary>
    /// Patches for WayBetterRomance RomanceUtilities.SexualityFactor compatibility with nonbinary gender
    /// </summary>
    [HarmonyPatch]
    public static class WayBetterRomanceSexualityFactorPatch
    {
        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            return WayBetterRomancePatchUtilities.StandardPrepare();
        }

        /// <summary>
        /// Dynamically targets BetterRomance.RomanceUtilities.SexualityFactor
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            return WayBetterRomancePatchUtilities.GetUtilityMethod("BetterRomance.RomanceUtilities", "SexualityFactor");
        }

        /// <summary>
        /// Prefix patch that handles non-binary gender for sexuality factor calculations
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Prefix(Pawn pawn, Pawn target, ref float __result)
        {
            // If either pawn is non-binary, use universal attraction logic
            if (SimpleTransHediffs.IsEnby(pawn) || SimpleTransHediffs.IsEnby(target))
            {
                // Non-binary pawns use universal attraction (no sexuality penalty)
                // Unless they're aromantic, in which case they get a penalty
                if (NonBinaryAttractionUtility.IsAromantic(pawn))
                {
                    __result = 0.1f; // Low attraction for aromantic pawns
                    SimpleTransDebug.Log($"WayBetterRomance SexualityFactor patch: Aromantic {pawn.Name} low attraction to {target.Name}", 2);
                }
                else
                {
                    __result = 1.0f; // Full attraction for non-aromantic NBG pawns
                    SimpleTransDebug.Log($"WayBetterRomance SexualityFactor patch: {pawn.Name} full attraction to {target.Name}", 2);
                }
                return false; // Skip original method
            }

            return true; // Continue with original method for binary genders
        }
    }

    /// <summary>
    /// Patches for WayBetterRomance RomanceUtilities.GetFirstLoverOfOppositeGender compatibility with nonbinary gender
    /// </summary>
    [HarmonyPatch]
    public static class WayBetterRomanceGetFirstLoverOfOppositeGenderPatch
    {
        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            return WayBetterRomancePatchUtilities.WayBetterRomancePrepare();
        }

        /// <summary>
        /// Dynamically targets BetterRomance.RomanceUtilities.GetFirstLoverOfOppositeGender
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            return WayBetterRomancePatchUtilities.GetUtilityMethod("BetterRomance.RomanceUtilities", "GetFirstLoverOfOppositeGender");
        }

        /// <summary>
        /// Prefix patch that handles non-binary gender for opposite gender lover finding
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Prefix(Pawn pawn, ref Pawn __result)
        {
            // If pawn is non-binary, return any lover (no "opposite gender" concept)
            if (SimpleTransHediffs.IsEnby(pawn))
            {
                __result = LovePartnerRelationUtility.ExistingLovePartner(pawn);
                SimpleTransDebug.Log($"WayBetterRomance GetFirstLoverOfOppositeGender patch: NBG {pawn.Name} returns any lover: {__result?.Name}", 2);
                return false; // Skip original method
            }

            // For binary genders, check if they have NBG lovers
            var lovers = pawn.relations.DirectRelations
                .Where(r => LovePartnerRelationUtility.IsLovePartnerRelation(r.def))
                .Select(r => r.otherPawn)
                .Where(p => p != null && !p.Dead);

            foreach (var lover in lovers)
            {
                if (SimpleTransHediffs.IsEnby(lover))
                {
                    __result = lover; // NBG lovers count as "opposite" to binary pawns
                    SimpleTransDebug.Log($"WayBetterRomance GetFirstLoverOfOppositeGender patch: Binary {pawn.Name} has NBG lover {lover.Name}", 2);
                    return false; // Skip original method
                }
            }

            return true; // Continue with original method for binary-binary relationships
        }
    }

    /// <summary>
    /// Patches for WayBetterRomance HookupUtility.HookupEligiblePair compatibility with nonbinary gender
    /// </summary>
    [HarmonyPatch]
    public static class WayBetterRomanceHookupEligiblePairPatch
    {
        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            return WayBetterRomancePatchUtilities.StandardPrepare();
        }

        /// <summary>
        /// Dynamically targets BetterRomance.HookupUtility.HookupEligiblePair
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            return WayBetterRomancePatchUtilities.GetUtilityMethod("BetterRomance.HookupUtility", "HookupEligiblePair");
        }

        /// <summary>
        /// Prefix patch that handles non-binary gender for hookup eligibility
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Prefix(Pawn initiator, Pawn target, bool forOpinionExplanation, ref AcceptanceReport __result)
        {
            // If either pawn is non-binary, use universal attraction logic
            if (SimpleTransHediffs.IsEnby(initiator) || SimpleTransHediffs.IsEnby(target))
            {
                // Check basic eligibility first
                if (initiator == target || initiator.Dead || target.Dead)
                {
                    __result = AcceptanceReport.WasRejected;
                    return false;
                }

                // Check if both are adults
                if (!initiator.ageTracker.Adult || !target.ageTracker.Adult)
                {
                    __result = AcceptanceReport.WasRejected;
                    return false;
                }

                // Check mutual attraction for NBG pawns
                bool initiatorAttracted = !NonBinaryAttractionUtility.IsAromantic(initiator);
                bool targetAttracted = !NonBinaryAttractionUtility.IsAromantic(target);
                
                __result = (initiatorAttracted && targetAttracted) ? AcceptanceReport.WasAccepted : AcceptanceReport.WasRejected;
                SimpleTransDebug.Log($"WayBetterRomance HookupEligiblePair patch: NBG {initiator.Name} and {target.Name} eligible: {__result.Accepted}", 2);
                return false; // Skip original method
            }

            return true; // Continue with original method for binary genders
        }
    }

    /// <summary>
    /// Patches for WayBetterRomance DateUtility.IsDateAppealing compatibility with nonbinary gender
    /// </summary>
    [HarmonyPatch]
    public static class WayBetterRomanceDateAppealPatch
    {
        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            return WayBetterRomancePatchUtilities.StandardPrepare();
        }

        /// <summary>
        /// Dynamically targets BetterRomance.DateUtility.IsDateAppealing
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            return WayBetterRomancePatchUtilities.GetUtilityMethod("BetterRomance.DateUtility", "IsDateAppealing");
        }

        /// <summary>
        /// Prefix patch that handles non-binary gender for date appeal calculations
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Prefix(Pawn target, Pawn asker, ref bool __result)
        {
            // If either pawn is non-binary, use universal attraction logic for dating
            if (SimpleTransHediffs.IsEnby(target) || SimpleTransHediffs.IsEnby(asker))
            {
                // Basic eligibility checks
                if (target == asker || target.Dead || asker.Dead)
                {
                    __result = false;
                    return false;
                }

                // Check if both are adults
                if (!target.ageTracker.Adult || !asker.ageTracker.Adult)
                {
                    __result = false;
                    return false;
                }

                // Check if target is colonist and available
                if (!target.IsColonist || target.Downed || target.InMentalState)
                {
                    __result = false;
                    return false;
                }

                // NBG pawns find dates appealing unless they're aromantic
                bool targetAttracted = !NonBinaryAttractionUtility.IsAromantic(target);
                bool askerAttracted = !NonBinaryAttractionUtility.IsAromantic(asker);

                // Both need to be attracted for date to be appealing
                __result = targetAttracted && askerAttracted;
                SimpleTransDebug.Log($"WayBetterRomance DateAppeal patch: {target.Name} finds date with {asker.Name} appealing: {__result}", 2);
                return false; // Skip original method
            }

            return true; // Continue with original method for binary genders
        }
    }

    /// <summary>
    /// Patches for WayBetterRomance SexualityUtility.CouldWeBeMarried compatibility with nonbinary gender
    /// </summary>
    [HarmonyPatch]
    public static class WayBetterRomanceCouldWeBeMarriedPatch
    {
        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            return WayBetterRomancePatchUtilities.StandardPrepare();
        }

        /// <summary>
        /// Dynamically targets BetterRomance.SexualityUtility.CouldWeBeMarried
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            return WayBetterRomancePatchUtilities.GetUtilityMethod("BetterRomance.SexualityUtility", "CouldWeBeMarried");
        }

        /// <summary>
        /// Prefix patch that handles non-binary gender for marriage compatibility
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Prefix(Pawn first, Pawn second, ref bool __result)
        {
            // If either pawn is non-binary, use universal compatibility logic
            if (SimpleTransHediffs.IsEnby(first) || SimpleTransHediffs.IsEnby(second))
            {
                // Basic checks
                if (first == second || first.Dead || second.Dead)
                {
                    __result = false;
                    return false;
                }

                // NBG pawns can marry anyone unless they're aromantic
                bool firstCanMarry = !NonBinaryAttractionUtility.IsAromantic(first);
                bool secondCanMarry = !NonBinaryAttractionUtility.IsAromantic(second);

                __result = firstCanMarry && secondCanMarry;
                SimpleTransDebug.Log($"WayBetterRomance CouldWeBeMarried patch: {first.Name} and {second.Name} can marry: {__result}", 2);
                return false; // Skip original method
            }

            return true; // Continue with original method for binary genders
        }
    }

    /// <summary>
    /// Patches for WayBetterRomance SexualityUtility.CouldWeBeLovers compatibility with nonbinary gender
    /// </summary>
    [HarmonyPatch]
    public static class WayBetterRomanceCouldWeBeLoversPatch
    {
        /// <summary>
        /// Determines if this patch should be applied
        /// </summary>
        public static bool Prepare()
        {
            return WayBetterRomancePatchUtilities.StandardPrepare();
        }

        /// <summary>
        /// Dynamically targets BetterRomance.SexualityUtility.CouldWeBeLovers
        /// </summary>
        public static System.Reflection.MethodBase TargetMethod()
        {
            return WayBetterRomancePatchUtilities.GetUtilityMethod("BetterRomance.SexualityUtility", "CouldWeBeLovers");
        }

        /// <summary>
        /// Prefix patch that handles non-binary gender for lover compatibility
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Prefix(Pawn first, Pawn second, ref bool __result)
        {
            // If either pawn is non-binary, use universal compatibility logic
            if (SimpleTransHediffs.IsEnby(first) || SimpleTransHediffs.IsEnby(second))
            {
                // Basic checks
                if (first == second || first.Dead || second.Dead)
                {
                    __result = false;
                    return false;
                }

                // NBG pawns can be lovers with anyone unless they're aromantic
                bool firstCanLove = !NonBinaryAttractionUtility.IsAromantic(first);
                bool secondCanLove = !NonBinaryAttractionUtility.IsAromantic(second);

                __result = firstCanLove && secondCanLove;
                SimpleTransDebug.Log($"WayBetterRomance CouldWeBeLovers patch: {first.Name} and {second.Name} can be lovers: {__result}", 2);
                return false; // Skip original method
            }

            return true; // Continue with original method for binary genders
        }
    }
}
