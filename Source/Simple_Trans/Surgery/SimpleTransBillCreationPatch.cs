using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Simple_Trans
{
    /// <summary>
    /// Patches bill stack to intercept Simple Trans surgery bill creation and show pregnancy warnings
    /// </summary>
    [HarmonyPatch(typeof(BillStack), "AddBill")]
    public static class SimpleTransSurgeryPregnancyPatch
    {
        // Flag to prevent infinite recursion when adding confirmed bills
        private static bool isAddingConfirmedBill = false;

        public static bool Prefix(BillStack __instance, Bill bill)
        {
            // Skip check if we're adding a confirmed bill
            if (isAddingConfirmedBill)
                return true;

            // Only intercept our Simple Trans surgeries that affect pregnancy
            if (!IsPregnancyAffectingSurgery(bill.recipe))
                return true; // Let other bills proceed normally

            // Get the pawn this bill is for (BillStack.billGiver should be the pawn for medical bills)
            if (!(__instance.billGiver is Pawn pawn))
                return true; // Not a pawn, proceed normally

            // Check if pawn is pregnant
            if (RimWorld.PregnancyUtility.GetPregnancyHediff(pawn) == null)
                return true; // Not pregnant, proceed normally

            // Show pregnancy confirmation dialog
            ShowPregnancyConfirmationDialog(pawn, bill, __instance);
            return false; // Prevent the bill from being added until user confirms
        }

        private static bool IsPregnancyAffectingSurgery(RecipeDef recipe)
        {
            // Check if this is one of our surgeries that affects carry ability
            return recipe.workerClass?.FullName == "Simple_Trans.Recipe_RemoveCarryAbility" ||
                   recipe.workerClass?.FullName == "Simple_Trans.Recipe_ReplaceReproductiveAbility";
        }

        private static void ShowPregnancyConfirmationDialog(Pawn pawn, Bill bill, BillStack billStack)
        {
            string warningText = "SimpleTrans.Surgery.PregnancyWarning".Translate(
                pawn.Named("PAWN"), 
                bill.recipe.LabelCap);

            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                warningText,
                delegate 
                {
                    // User confirmed - add the bill without triggering our patch again
                    isAddingConfirmedBill = true;
                    try
                    {
                        billStack.AddBill(bill);
                    }
                    finally
                    {
                        isAddingConfirmedBill = false;
                    }
                },
                destructive: true
            ));
        }
    }
}