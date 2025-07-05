using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Simple_Trans
{
    /// <summary>
    /// Informed consent dialog for Simple Trans biosculpter cycles
    /// Shows exactly what will happen to the pawn before transformation
    /// </summary>
    public class SimpleTransDialog_TransformationConsent : Window
    {
        private readonly Pawn pawn;
        private readonly CompBiosculpterPod_Cycle cycle;
        private readonly Action proceedAction;
        private readonly string cycleLabel;
        private readonly List<TransformationChange> changes;
        
        private Vector2 scrollPosition = Vector2.zero;
        public override Vector2 InitialSize => new Vector2(600f, 500f);

        public static void CreateDialog(Pawn pawn, CompBiosculpterPod_Cycle cycle, string cycleLabel, Action proceedAction)
        {
            if (pawn == null)
            {
                Log.Error($"Attempted creating {nameof(SimpleTransDialog_TransformationConsent)} with null pawn");
                return;
            }

            var changes = AnalyzeTransformationChanges(pawn, cycle);
            if (changes.Count > 0)
            {
                Find.WindowStack.Add(new SimpleTransDialog_TransformationConsent(pawn, cycle, cycleLabel, changes, proceedAction));
            }
            else
            {
                // No changes would occur - ask if they want to proceed anyway
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "SimpleTrans.NoChangesConfirm".Translate(pawn.Named("PAWN"), cycleLabel),
                    proceedAction));
            }
        }

        private SimpleTransDialog_TransformationConsent(Pawn pawn, CompBiosculpterPod_Cycle cycle, string cycleLabel, List<TransformationChange> changes, Action proceedAction)
        {
            this.pawn = pawn;
            this.cycle = cycle;
            this.cycleLabel = cycleLabel;
            this.changes = changes;
            this.proceedAction = proceedAction;

            forcePause = true;
            absorbInputAroundWindow = true;
            onlyOneOfTypeAllowed = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var oriColor = GUI.color;
            var oriFont = Text.Font;

            float y = inRect.y;
            
            // Title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, inRect.width, 42f), "SimpleTrans.ConsentDialogTitle".Translate(cycleLabel));
            y += 35f;

            // Subtitle with pawn name
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, y, inRect.width, 28f), "SimpleTrans.ConsentDialogSubtitle".Translate(pawn.Named("PAWN")));
            y += 35f;

            // Changes section
            Text.Font = GameFont.Small;
            var changesLabel = "SimpleTrans.ConsentChangesHeader".Translate();
            Widgets.Label(new Rect(0f, y, inRect.width, 25f), changesLabel);
            y += 30f;

            // Scrollable changes list
            Rect outRect = new Rect(inRect.x, y, inRect.width, inRect.height - 100f - y);
            float contentHeight = CalculateContentHeight(outRect.width);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, contentHeight);
            
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            
            float scrollY = 0f;
            foreach (var change in changes)
            {
                DrawTransformationChange(new Rect(10f, scrollY, viewRect.width - 20f, 25f), change);
                scrollY += 30f;
            }
            
            Widgets.EndScrollView();

            // Warning text
            y = inRect.height - 85f;
            GUI.color = Color.yellow;
            Text.Font = GameFont.Tiny;
            var warningText = "SimpleTrans.ConsentWarning".Translate();
            float warningHeight = Text.CalcHeight(warningText, inRect.width);
            Widgets.Label(new Rect(0f, y, inRect.width, warningHeight), warningText);
            GUI.color = oriColor;

            // Buttons
            y = inRect.height - 35f;
            if (Widgets.ButtonText(new Rect(0f, y, inRect.width / 2f - 10f, 35f), "CancelButton".Translate()))
            {
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.width / 2f + 10f, y, inRect.width / 2f - 10f, 35f), "SimpleTrans.ConsentProceed".Translate()))
            {
                Close();
                proceedAction.Invoke();
            }

            Text.Font = oriFont;
            GUI.color = oriColor;
        }

        private void DrawTransformationChange(Rect rect, TransformationChange change)
        {
            var oriColor = GUI.color;
            
            // Color coding for change types
            switch (change.Type)
            {
                case TransformationChangeType.Addition:
                    GUI.color = Color.green;
                    break;
                case TransformationChangeType.Removal:
                    GUI.color = new Color(1f, 0.5f, 0.5f); // Light red
                    break;
                case TransformationChangeType.Modification:
                    GUI.color = Color.yellow;
                    break;
                default:
                    GUI.color = Color.white;
                    break;
            }

            // Icon
            string icon = change.Type switch
            {
                TransformationChangeType.Addition => "+",
                TransformationChangeType.Removal => "-",
                TransformationChangeType.Modification => "~",
                _ => "•"
            };
            
            Widgets.Label(new Rect(rect.x, rect.y, 20f, rect.height), icon);
            
            // Description
            GUI.color = oriColor;
            Widgets.Label(new Rect(rect.x + 25f, rect.y, rect.width - 25f, rect.height), change.Description);
        }

        private float CalculateContentHeight(float width)
        {
            return changes.Count * 30f + 10f;
        }

        /// <summary>
        /// Analyzes what changes would occur to a pawn during transformation
        /// </summary>
        private static List<TransformationChange> AnalyzeTransformationChanges(Pawn pawn, CompBiosculpterPod_Cycle cycle)
        {
            var changes = new List<TransformationChange>();

            // We need to cast to our specific cycle types to analyze changes
            switch (cycle)
            {
                case CompBiosculpterPod_ReproductiveReconstructionMasculinizing:
                    AnalyzeMasculinizingChanges(pawn, changes);
                    break;
                case CompBiosculpterPod_ReproductiveReconstructionFeminizing:
                    AnalyzeFeminizingChanges(pawn, changes);
                    break;
                case CompBiosculpterPod_FertilityRestoration:
                    AnalyzeFertilityRestorationChanges(pawn, changes);
                    break;
                case CompBiosculpterPod_Androgynize:
                    AnalyzeAndrogynizeChanges(pawn, changes);
                    break;
                case CompBiosculpterPod_Duosex:
                    AnalyzeDuosexChanges(pawn, changes);
                    break;
            }

            return changes;
        }

        private static void AnalyzeMasculinizingChanges(Pawn pawn, List<TransformationChange> changes)
        {
            // Gender change
            if (pawn.gender != Gender.Male)
                changes.Add(new TransformationChange(TransformationChangeType.Modification, "SimpleTrans.Change.SetGenderMale".Translate()));

            // Body type change
            if (pawn.story?.bodyType != BodyTypeDefOf.Male)
                changes.Add(new TransformationChange(TransformationChangeType.Modification, "SimpleTrans.Change.SetBodyTypeMale".Translate()));

            // Reproductive abilities
            bool hasCarry = SimpleTransPregnancyUtility.CanCarry(pawn);
            bool hasSire = SimpleTransPregnancyUtility.CanSire(pawn);

            if (hasCarry)
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.RemoveCarryingAbility".Translate()));
            
            if (!hasSire)
                changes.Add(new TransformationChange(TransformationChangeType.Addition, "SimpleTrans.Change.AddSiringAbility".Translate()));

            // Prosthetics removal
            AnalyzeProstheticsRemoval(pawn, changes);

            // Gender identity
            bool isCis = pawn.health.hediffSet.HasHediff(SimpleTransPregnancyUtility.cisDef);
            if (!isCis)
                changes.Add(new TransformationChange(TransformationChangeType.Modification, "SimpleTrans.Change.SetCisgender".Translate()));
        }

        private static void AnalyzeFeminizingChanges(Pawn pawn, List<TransformationChange> changes)
        {
            // Gender change
            if (pawn.gender != Gender.Female)
                changes.Add(new TransformationChange(TransformationChangeType.Modification, "SimpleTrans.Change.SetGenderFemale".Translate()));

            // Body type change  
            if (pawn.story?.bodyType != BodyTypeDefOf.Female)
                changes.Add(new TransformationChange(TransformationChangeType.Modification, "SimpleTrans.Change.SetBodyTypeFemale".Translate()));

            // Reproductive abilities
            bool hasCarry = SimpleTransPregnancyUtility.CanCarry(pawn);
            bool hasSire = SimpleTransPregnancyUtility.CanSire(pawn);

            if (hasSire)
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.RemoveSiringAbility".Translate()));
            
            if (!hasCarry)
                changes.Add(new TransformationChange(TransformationChangeType.Addition, "SimpleTrans.Change.AddCarryingAbility".Translate()));

            // Prosthetics removal
            AnalyzeProstheticsRemoval(pawn, changes);

            // Gender identity
            bool isCis = pawn.health.hediffSet.HasHediff(SimpleTransPregnancyUtility.cisDef);
            if (!isCis)
                changes.Add(new TransformationChange(TransformationChangeType.Modification, "SimpleTrans.Change.SetCisgender".Translate()));
        }

        private static void AnalyzeFertilityRestorationChanges(Pawn pawn, List<TransformationChange> changes)
        {
            // Only prosthetics removal and natural restoration
            AnalyzeProstheticsRemoval(pawn, changes);

            // Check if natural abilities would be restored
            bool hasCarry = SimpleTransPregnancyUtility.CanCarry(pawn);
            bool hasSire = SimpleTransPregnancyUtility.CanSire(pawn);

            if (!hasCarry && pawn.gender == Gender.Female)
                changes.Add(new TransformationChange(TransformationChangeType.Addition, "SimpleTrans.Change.RestoreNaturalCarrying".Translate()));
            
            if (!hasSire && pawn.gender == Gender.Male)
                changes.Add(new TransformationChange(TransformationChangeType.Addition, "SimpleTrans.Change.RestoreNaturalSiring".Translate()));
        }

        private static void AnalyzeAndrogynizeChanges(Pawn pawn, List<TransformationChange> changes)
        {
            // Gender change to non-binary (if NBG mod loaded)
            bool hasNBGMod = ModsConfig.IsActive("Coraizon.NonBinaryGenderMod") || ModsConfig.IsActive("Coraizon.NBGM");
            if (hasNBGMod && (int)pawn.gender != 3)
                changes.Add(new TransformationChange(TransformationChangeType.Modification, "SimpleTrans.Change.SetGenderNonBinary".Translate()));

            // Body type to thin
            if (pawn.story?.bodyType != BodyTypeDefOf.Thin)
                changes.Add(new TransformationChange(TransformationChangeType.Modification, "SimpleTrans.Change.SetBodyTypeThin".Translate()));

            // Remove all reproductive abilities
            if (SimpleTransPregnancyUtility.CanCarry(pawn))
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.RemoveCarryingAbility".Translate()));
            
            if (SimpleTransPregnancyUtility.CanSire(pawn))
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.RemoveSiringAbility".Translate()));

            // Prosthetics removal
            AnalyzeProstheticsRemoval(pawn, changes);

            // Gender identity to transgender
            bool isTrans = pawn.health.hediffSet.HasHediff(SimpleTransPregnancyUtility.transDef);
            if (!isTrans)
                changes.Add(new TransformationChange(TransformationChangeType.Modification, "SimpleTrans.Change.SetTransgender".Translate()));
        }

        private static void AnalyzeDuosexChanges(Pawn pawn, List<TransformationChange> changes)
        {
            // Gender change to non-binary (if NBG mod loaded)
            bool hasNBGMod = ModsConfig.IsActive("Coraizon.NonBinaryGenderMod") || ModsConfig.IsActive("Coraizon.NBGM");
            if (hasNBGMod && (int)pawn.gender != 3)
                changes.Add(new TransformationChange(TransformationChangeType.Modification, "SimpleTrans.Change.SetGenderNonBinary".Translate()));

            // Body type to thin
            if (pawn.story?.bodyType != BodyTypeDefOf.Thin)
                changes.Add(new TransformationChange(TransformationChangeType.Modification, "SimpleTrans.Change.SetBodyTypeThin".Translate()));

            // Grant both reproductive abilities
            bool hasCarry = SimpleTransPregnancyUtility.CanCarry(pawn);
            bool hasSire = SimpleTransPregnancyUtility.CanSire(pawn);

            if (!hasCarry)
                changes.Add(new TransformationChange(TransformationChangeType.Addition, "SimpleTrans.Change.AddCarryingAbility".Translate()));
            
            if (!hasSire)
                changes.Add(new TransformationChange(TransformationChangeType.Addition, "SimpleTrans.Change.AddSiringAbility".Translate()));

            // Prosthetics removal
            AnalyzeProstheticsRemoval(pawn, changes);

            // Gender identity to transgender
            bool isTrans = pawn.health.hediffSet.HasHediff(SimpleTransPregnancyUtility.transDef);
            if (!isTrans)
                changes.Add(new TransformationChange(TransformationChangeType.Modification, "SimpleTrans.Change.SetTransgender".Translate()));
        }

        private static void AnalyzeProstheticsRemoval(Pawn pawn, List<TransformationChange> changes)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            
            if (hediffs.Any(h => h.def.defName == "BasicProstheticCarry"))
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.RemoveBasicCarryProsthetic".Translate()));
            
            if (hediffs.Any(h => h.def.defName == "BasicProstheticSire"))
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.RemoveBasicSireProsthetic".Translate()));
            
            if (hediffs.Any(h => h.def.defName == "BionicProstheticCarry"))
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.RemoveBionicCarryProsthetic".Translate()));
            
            if (hediffs.Any(h => h.def.defName == "BionicProstheticSire"))
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.RemoveBionicSireProsthetic".Translate()));
        }
    }

    public class TransformationChange
    {
        public TransformationChangeType Type { get; }
        public string Description { get; }

        public TransformationChange(TransformationChangeType type, string description)
        {
            Type = type;
            Description = description;
        }
    }

    public enum TransformationChangeType
    {
        Addition,
        Removal,
        Modification,
        Information
    }
}