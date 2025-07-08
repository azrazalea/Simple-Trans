using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace Simple_Trans
{
    /// <summary>
    /// Harmony patch to intercept Simple Trans biosculpter cycle orders
    /// and show informed consent dialog before proceeding
    /// Note: This patch is applied conditionally only when Ideology DLC is active
    /// </summary>
    public static class SimpleTransBiosculpterPatch
    {
        [HarmonyPrefix]
        public static bool OrderToPod_Prefix(ref CompBiosculpterPod_Cycle cycle, ref Pawn pawn, ref Action giveJobAct)
        {
            // Only intercept our Simple Trans cycles
            if (IsSimpleTransCycle(cycle))
            {
                // Gender Affirming Cycle gets special treatment - show choice dialog first
                if (cycle is CompBiosculpterPod_GenderAffirmingCycle genderAffirmingCycle)
                {
                    SimpleTransDialog_GenderAffirmingChoice.CreateDialog(pawn, genderAffirmingCycle, giveJobAct);
                    return false; // Prevent original method from running
                }
            }

            return true; // Allow other cycles to proceed normally
        }

        private static bool IsSimpleTransCycle(CompBiosculpterPod_Cycle cycle)
        {
            return cycle is CompBiosculpterPod_GenderAffirmingCycle;
        }

        private static string GetCycleLabel(CompBiosculpterPod_Cycle cycle)
        {
            return cycle switch
            {
                CompBiosculpterPod_GenderAffirmingCycle => "SimpleTrans.GenderAffirmingChoice.CycleLabel".Translate().Resolve(),
                _ => "Unknown Cycle"
            };
        }
    }
}
