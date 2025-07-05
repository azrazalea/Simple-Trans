using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace Simple_Trans
{
    /// <summary>
    /// Harmony patch to intercept Simple Trans biosculpter cycle orders
    /// and show informed consent dialog before proceeding
    /// </summary>
    [HarmonyPatch(typeof(CompBiosculpterPod), "OrderToPod")]
    public static class SimpleTransBiosculpterPatch
    {
        [HarmonyPrefix]
        public static bool OrderToPod_Prefix(ref CompBiosculpterPod_Cycle cycle, ref Pawn pawn, ref Action giveJobAct)
        {
            // Only intercept our Simple Trans cycles
            if (IsSimpleTransCycle(cycle))
            {
                string cycleLabel = GetCycleLabel(cycle);
                SimpleTransDialog_TransformationConsent.CreateDialog(pawn, cycle, cycleLabel, giveJobAct);
                return false; // Prevent original method from running
            }

            return true; // Allow other cycles to proceed normally
        }

        private static bool IsSimpleTransCycle(CompBiosculpterPod_Cycle cycle)
        {
            return cycle is CompBiosculpterPod_ReproductiveReconstructionMasculinizing ||
                   cycle is CompBiosculpterPod_ReproductiveReconstructionFeminizing ||
                   cycle is CompBiosculpterPod_FertilityRestoration ||
                   cycle is CompBiosculpterPod_Androgynize ||
                   cycle is CompBiosculpterPod_Duosex;
        }

        private static string GetCycleLabel(CompBiosculpterPod_Cycle cycle)
        {
            return cycle switch
            {
                CompBiosculpterPod_ReproductiveReconstructionMasculinizing => "SimpleTrans.Biosculpter.MasculinizingLabel".Translate(),
                CompBiosculpterPod_ReproductiveReconstructionFeminizing => "SimpleTrans.Biosculpter.FeminizingLabel".Translate(),
                CompBiosculpterPod_FertilityRestoration => "SimpleTrans.Biosculpter.FertilityRestorationLabel".Translate(),
                CompBiosculpterPod_Androgynize => "SimpleTrans.Biosculpter.AndrogynizeLabel".Translate(),
                CompBiosculpterPod_Duosex => "SimpleTrans.Biosculpter.DuosexLabel".Translate(),
                _ => "Unknown Cycle"
            };
        }
    }
}