using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace Simple_Trans.Patches
{
    [HarmonyPatch]
    public static class SurgeryPatches
    {
        [HarmonyPatch(typeof(PregnancyUtility), "PregnancyChanceForPartners")]
        [HarmonyPostfix]
        public static void PregnancyChanceForPartners_Postfix(Pawn woman, Pawn man, ref float __result)
        {
            if (__result <= 0f) return;
            
            Hediff basicProstheticFemale = woman.health?.hediffSet?.GetFirstHediffOfDef(HediffDef.Named("SimpleTransBasicProsthetic"));
            Hediff basicProstheticMale = man.health?.hediffSet?.GetFirstHediffOfDef(HediffDef.Named("SimpleTransBasicProsthetic"));
            Hediff advancedProstheticFemale = woman.health?.hediffSet?.GetFirstHediffOfDef(HediffDef.Named("SimpleTransAdvancedProsthetic"));
            Hediff advancedProstheticMale = man.health?.hediffSet?.GetFirstHediffOfDef(HediffDef.Named("SimpleTransAdvancedProsthetic"));
            
            if (basicProstheticFemale != null || basicProstheticMale != null)
            {
                __result *= 0.7f;
            }
            
            if (advancedProstheticFemale != null && advancedProstheticMale != null)
            {
                // Advanced prosthetics provide enhanced fertility when both partners have them
                __result *= 1.5f; // 50% bonus when both have advanced prosthetics
            }
            else if (advancedProstheticFemale != null || advancedProstheticMale != null)
            {
                // Single advanced prosthetic provides smaller bonus
                __result *= 1.2f; // 20% bonus for one advanced prosthetic
            }
        }
        
        [HarmonyPatch(typeof(PregnancyUtility), "CanEverProduceChild")]
        [HarmonyPostfix]
        public static void CanEverProduceChild_Postfix(Pawn first, Pawn second, ref AcceptanceReport __result)
        {
            if (__result.Accepted) return;
            
            bool firstCanCarry = SimpleTransPregnancyUtility.CanCarry(first);
            bool firstCanSire = SimpleTransPregnancyUtility.CanSire(first);
            bool secondCanCarry = SimpleTransPregnancyUtility.CanCarry(second);
            bool secondCanSire = SimpleTransPregnancyUtility.CanSire(second);
            
            if ((firstCanCarry && secondCanSire) || (firstCanSire && secondCanCarry))
            {
                __result = AcceptanceReport.WasAccepted;
            }
        }
        
        
        [HarmonyPatch(typeof(Recipe_Surgery), "AvailableOnNow")]
        [HarmonyPostfix]
        public static void Recipe_Surgery_AvailableOnNow_Postfix(Recipe_Surgery __instance, Thing thing, ref bool __result)
        {
            if (!__result || !(thing is Pawn pawn)) return;
            
            // Check if this is a Simple Trans surgery and if the relevant system is enabled
            string recipeName = __instance.recipe?.defName ?? "";
            if (recipeName.StartsWith("Extract") && recipeName.Contains("Organs"))
            {
                if (!SimpleTransPregnancyUtility.enableOrganTransplants)
                {
                    __result = false;
                    return;
                }
            }
            else if (recipeName.StartsWith("Transplant") && recipeName.Contains("Organs"))
            {
                if (!SimpleTransPregnancyUtility.enableOrganTransplants)
                {
                    __result = false;
                    return;
                }
            }
            else if (recipeName.Contains("ReproductiveProsthetic"))
            {
                if (!SimpleTransPregnancyUtility.enableProsthetics)
                {
                    __result = false;
                    return;
                }
            }
            
            SimpleTrans_SurgeryExtension extension = __instance.recipe?.GetModExtension<SimpleTrans_SurgeryExtension>();
            if (extension == null) return;
            
            if (extension.requiresCarryAbility && !SimpleTransPregnancyUtility.CanCarry(pawn))
            {
                __result = false;
            }
            
            if (extension.requiresSireAbility && !SimpleTransPregnancyUtility.CanSire(pawn))
            {
                __result = false;
            }
        }
    }
}