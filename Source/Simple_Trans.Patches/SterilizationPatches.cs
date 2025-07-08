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

    }
}
