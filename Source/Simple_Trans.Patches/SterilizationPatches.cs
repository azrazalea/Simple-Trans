using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Reflection;

namespace Simple_Trans.Patches
{
    [HarmonyPatch]
    public static class SterilizationPatches
    {
        // Patch vanilla surgery AvailableOnNow to add our capability and bionic checks
        [HarmonyPatch(typeof(Recipe_Surgery), "AvailableOnNow")]
        [HarmonyPostfix]
        public static void Recipe_Surgery_AvailableOnNow_Postfix(Recipe_Surgery __instance, Thing thing, ref bool __result, BodyPartRecord part)
        {
            // Only process if surgery was initially available and for pawns
            if (!__result || !(thing is Pawn pawn)) return;

            // Check if this is one of our capability-based surgeries
            if (__instance?.recipe == null) return;

            string defName = __instance.recipe.defName;

            // Block sterilization surgeries on bionics (just make unavailable, document explains why)
            if (defName == "TubalLigation" || defName == "Vasectomy" || defName == "ImplantIUD")
            {
                if (HasBionicReproductiveProsthetic(pawn, defName))
                {
                    __result = false;
                    return;
                }
            }

            // Surgeries that require carry ability
            if (defName == "TerminatePregnancy" || defName == "TubalLigation" || defName == "ImplantIUD" || defName == "RemoveIUD" || defName == "ExtractOvum")
            {
                if (!SimpleTransPregnancyUtility.CanCarry(pawn))
                {
                    __result = false;
                }
            }
            // Surgeries that require sire ability  
            else if (defName == "Vasectomy" || defName == "ReverseVasectomy")
            {
                if (!SimpleTransPregnancyUtility.CanSire(pawn))
                {
                    __result = false;
                }
            }
        }

        /// <summary>
        /// Checks if a pawn has bionic reproductive prosthetics for the surgery type
        /// </summary>
        /// <param name="pawn">The pawn to check</param>
        /// <param name="surgeryType">The surgery being attempted</param>
        /// <returns>True if pawn has relevant bionic prosthetics</returns>
        private static bool HasBionicReproductiveProsthetic(Pawn pawn, string surgeryType)
        {
            if (pawn?.health?.hediffSet == null) return false;

            var bionicCarryDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicProstheticCarry");
            var bionicSireDef = DefDatabase<HediffDef>.GetNamedSilentFail("BionicProstheticSire");

            switch (surgeryType)
            {
                case "TubalLigation":
                case "ImplantIUD":
                    return bionicCarryDef != null && pawn.health.hediffSet.HasHediff(bionicCarryDef);
                case "Vasectomy":
                    return bionicSireDef != null && pawn.health.hediffSet.HasHediff(bionicSireDef);
                default:
                    return false;
            }
        }

        // Patch Recipe_ImplantEmbryo to check carry ability
        [HarmonyPatch(typeof(Recipe_ImplantEmbryo), "CompletableEver")]
        [HarmonyPostfix]
        public static void Recipe_ImplantEmbryo_CompletableEver_Postfix(Pawn surgeryTarget, ref bool __result)
        {
            if (!__result) return;

            // Override with carry ability check
            if (!SimpleTransPregnancyUtility.CanCarry(surgeryTarget))
            {
                __result = false;
            }
        }

        // Patch Recipe_ExtractOvum to check carry ability instead of vanilla fertility/gender
        [HarmonyPatch(typeof(Recipe_ExtractOvum), "CompletableEver")]
        [HarmonyPostfix]
        public static void Recipe_ExtractOvum_CompletableEver_Postfix(Pawn surgeryTarget, ref bool __result)
        {
            if (!__result) return;

            // Override with carry ability check
            if (!SimpleTransPregnancyUtility.CanCarry(surgeryTarget))
            {
                __result = false;
            }
        }

        // Replace vanilla CanFertilizeReport with our sirer-based implementation
        [HarmonyPatch(typeof(HumanOvum), "CanFertilizeReport")]
        [HarmonyPrefix]
        public static bool HumanOvum_CanFertilizeReport_Prefix(Pawn pawn, ref AcceptanceReport __result)
        {
            // Replace the entire method with our implementation
            __result = CanFertilizeReport_SimpleTrans(pawn);
            return false; // Skip original method
        }

        /// <summary>
        /// Simple Trans replacement for HumanOvum.CanFertilizeReport
        /// Checks sirer capability instead of vanilla gender/sterility with specific error messages
        /// </summary>
        /// <param name="pawn">The pawn to check</param>
        /// <returns>AcceptanceReport indicating if pawn can fertilize</returns>
        private static AcceptanceReport CanFertilizeReport_SimpleTrans(Pawn pawn)
        {
            // Check if pawn can sire (this handles both capability and sterility checks with specific reasons)
            AcceptanceReport sireReport = SimpleTransPregnancyUtility.CanSireReport(pawn);
            if (!sireReport.Accepted)
            {
                return sireReport;
            }

            // Check if pawn is quest lodger
            if (pawn.IsQuestLodger())
            {
                return false;
            }

            // Check if pawn can reach the ovum (this requires the ovum instance, but we'll skip path check in prefix)
            // The path check will be handled by the gizmo/float menu system

            // Check age requirement
            if ((float)pawn.ageTracker.AgeBiologicalYears < 14f)
            {
                return "CannotMustBeAge".Translate(14f).CapitalizeFirst();
            }

            // Check if pawn is incapacitated
            if (pawn.Downed || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return "Incapacitated".Translate().ToLower();
            }

            return true;
        }

    }
}
