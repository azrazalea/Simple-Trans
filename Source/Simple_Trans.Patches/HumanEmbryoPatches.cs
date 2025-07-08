using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;

namespace Simple_Trans.Patches
{
    [HarmonyPatch]
    public static class HumanEmbryoPatches
    {
        // Postfix to override gender check with capability check
        [HarmonyPatch(typeof(HumanEmbryo), "CanImplantReport")]
        [HarmonyPostfix]
        public static void HumanEmbryo_CanImplantReport_Postfix(ref AcceptanceReport __result, Pawn pawn)
        {
            // If the original method rejected due to gender, check if we can override with capability
            if (!__result.Accepted)
            {
                // Check if this pawn can carry pregnancies regardless of gender
                if (SimpleTransPregnancyUtility.CanCarry(pawn))
                {
                    // Re-run all the checks except gender
                    if (SimpleTrans.debugMode)
                    {
                        Log.Message($"[Simple Trans DEBUG] HumanEmbryo.CanImplantReport: Overriding gender rejection for {pawn?.Name?.ToStringShort ?? "unknown"} - checking carry capability");
                    }
                    
                    // Perform all the same checks as the original method, but skip gender
                    if (pawn.IsQuestLodger())
                    {
                        __result = false;
                        return;
                    }
                    
                    HashSet<Pawn> reservers = new HashSet<Pawn>();
                    pawn.Map?.reservationManager?.ReserversOf(pawn, reservers);
                    if (reservers.Any())
                    {
                        Pawn reserver = reservers.First();
                        __result = "ReservedBy".Translate(reserver.LabelShort, reserver);
                        return;
                    }
                    
                    if (pawn.BillStack.Bills.Any(b => b.recipe == RecipeDefOf.ImplantEmbryo))
                    {
                        __result = "CannotImplantingOtherEmbryo".Translate();
                        return;
                    }
                    
                    if (pawn.ageTracker.AgeBiologicalYears < 16)
                    {
                        __result = "CannotMustBeAge".Translate(16).CapitalizeFirst();
                        return;
                    }
                    
                    if (pawn.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman))
                    {
                        __result = "CannotPregnant".Translate();
                        return;
                    }
                    
                    // Check if the pawn can carry pregnancies (not sterilized for carrying)
                    if (!SimpleTransPregnancyUtility.CanCarry(pawn))
                    {
                        __result = "CannotSterile".Translate();
                        return;
                    }
                    
                    // All checks passed - accept the implantation
                    __result = true;
                    
                    if (SimpleTrans.debugMode)
                    {
                        Log.Message($"[Simple Trans DEBUG] HumanEmbryo.CanImplantReport: ACCEPTED {pawn?.Name?.ToStringShort ?? "unknown"} for embryo implantation based on carry capability");
                    }
                }
            }
            else if (SimpleTrans.debugMode)
            {
                Log.Message($"[Simple Trans DEBUG] HumanEmbryo.CanImplantReport: {pawn?.Name?.ToStringShort ?? "unknown"} - original result: ACCEPTED");
            }
        }
        
        // Patch HumanEmbryo's GetGizmos to fix "no women" message
        [HarmonyPatch(typeof(HumanEmbryo), "GetGizmos")]
        [HarmonyPostfix]
        public static void HumanEmbryo_GetGizmos_Postfix(HumanEmbryo __instance, ref IEnumerable<Gizmo> __result)
        {
            // We need to replace any gizmos that mention "no women" with "no carriers"
            var gizmos = new List<Gizmo>(__result);
            
            for (int i = 0; i < gizmos.Count; i++)
            {
                if (gizmos[i] is Command_Action command && command.disabledReason != null)
                {
                    // Check if this is the "no women" disable reason
                    if (command.disabledReason.Contains("ImplantDisabledNoWomen") || 
                        command.disabledReason.Contains("no women") ||
                        command.disabledReason.Contains("No women"))
                    {
                        // Replace with carry-based message
                        command.disabledReason = "SimpleTrans.ImplantDisabledNoCarriers".Translate();
                    }
                }
            }
            
            __result = gizmos;
        }
    }
}