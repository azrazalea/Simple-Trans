using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace Simple_Trans
{
    [HarmonyPatch]
    public static class HumanEmbryoPatches
    {
        /// <summary>
        /// Patch HumanOvum.ProduceEmbryo to store explicit parent roles in our custom comp.
        /// The ovum's pawn source is the carrier, and the father parameter is the sirer.
        /// </summary>
        [HarmonyPatch(typeof(HumanOvum), nameof(HumanOvum.ProduceEmbryo))]
        [HarmonyPostfix]
        public static void HumanOvum_ProduceEmbryo_Postfix(HumanOvum __instance, Pawn father, Thing __result)
        {
            if (__result == null || !(__result is HumanEmbryo embryo))
                return;

            var parentComp = embryo.TryGetComp<CompEmbryoParentInfo>();
            if (parentComp == null)
            {
                SimpleTransDebug.Log("CompEmbryoParentInfo not found on embryo - XML patch may not be loaded", 1);
                return;
            }

            // Get the ovum provider (carrier) from the ovum's pawn sources
            var ovumSources = __instance.GetComp<CompHasPawnSources>();
            Pawn carrier = ovumSources?.pawnSources?.FirstOrDefault();

            // Store the explicit parent roles
            parentComp.carrier = carrier;
            parentComp.sirer = father;

            SimpleTransDebug.Log($"Embryo created - Carrier: {carrier?.Name?.ToStringShort ?? "unknown"}, Sirer: {father?.Name?.ToStringShort ?? "unknown"}", 1);
        }

        /// <summary>
        /// Patch to fix Mother property - should return the carrier (ovum provider), not just female gender
        /// </summary>
        [HarmonyPatch(typeof(HumanEmbryo), nameof(HumanEmbryo.Mother), MethodType.Getter)]
        [HarmonyPostfix]
        public static void HumanEmbryo_Mother_Postfix(HumanEmbryo __instance, ref Pawn __result)
        {
            // First try to use our explicit parent tracking
            var parentComp = __instance.TryGetComp<CompEmbryoParentInfo>();
            if (parentComp?.carrier != null)
            {
                __result = parentComp.carrier;
                SimpleTransDebug.Log($"Mother from comp: {parentComp.carrier.Name?.ToStringShort}", 2);
                return;
            }

            // Fallback for embryos created before this patch: find pawn with carry capability
            var pawnSourcesComp = __instance.TryGetComp<CompHasPawnSources>();
            if (pawnSourcesComp?.pawnSources == null || pawnSourcesComp.pawnSources.Count == 0)
                return;

            // Find the pawn with carry capability hediff
            Pawn carrier = pawnSourcesComp.pawnSources.FirstOrDefault(p =>
                p?.health?.hediffSet?.HasHediff(SimpleTransHediffs.canCarryDef, false) == true);

            if (carrier != null)
            {
                __result = carrier;
                SimpleTransDebug.Log($"Mother fallback: {carrier.Name?.ToStringShort} (has carry hediff)", 2);
            }
        }

        /// <summary>
        /// Patch to fix Father property - should return the sirer (fertilizer), not just male gender
        /// </summary>
        [HarmonyPatch(typeof(HumanEmbryo), nameof(HumanEmbryo.Father), MethodType.Getter)]
        [HarmonyPostfix]
        public static void HumanEmbryo_Father_Postfix(HumanEmbryo __instance, ref Pawn __result)
        {
            // First try to use our explicit parent tracking
            var parentComp = __instance.TryGetComp<CompEmbryoParentInfo>();
            if (parentComp?.sirer != null)
            {
                __result = parentComp.sirer;
                SimpleTransDebug.Log($"Father from comp: {parentComp.sirer.Name?.ToStringShort}", 2);
                return;
            }

            // Fallback for embryos created before this patch: find pawn with sire capability
            var pawnSourcesComp = __instance.TryGetComp<CompHasPawnSources>();
            if (pawnSourcesComp?.pawnSources == null || pawnSourcesComp.pawnSources.Count == 0)
                return;

            // Find the pawn with sire capability hediff
            Pawn sirer = pawnSourcesComp.pawnSources.FirstOrDefault(p =>
                p?.health?.hediffSet?.HasHediff(SimpleTransHediffs.canSireDef, false) == true);

            if (sirer != null)
            {
                __result = sirer;
                SimpleTransDebug.Log($"Father fallback: {sirer.Name?.ToStringShort} (has sire hediff)", 2);
            }
        }

        /// <summary>
        /// Postfix to override gender check with capability check for embryo implantation
        /// </summary>
        [HarmonyPatch(typeof(HumanEmbryo), "CanImplantReport")]
        [HarmonyPostfix]
        public static void HumanEmbryo_CanImplantReport_Postfix(ref AcceptanceReport __result, Pawn pawn)
        {
            // If the original method rejected due to gender, check if we can override with capability
            if (!__result.Accepted)
            {
                // Check if this pawn can carry pregnancies regardless of gender
                if (SimpleTransHediffs.CanCarry(pawn))
                {
                    // Re-run all the checks except gender
                    SimpleTransDebug.Log($"Embryo implantation override: {pawn?.Name?.ToStringShort ?? "unknown"} - checking carry capability", 2);

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
                    if (!SimpleTransHediffs.CanCarry(pawn))
                    {
                        __result = "CannotSterile".Translate();
                        return;
                    }

                    __result = true;

                    SimpleTransDebug.Log($"Embryo implantation accepted: {pawn?.Name?.ToStringShort ?? "unknown"} (carry capability)", 1);
                }
            }
        }

        /// <summary>
        /// Patch HumanEmbryo's GetGizmos to fix "no women" message
        /// </summary>
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
