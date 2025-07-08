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
        public override Vector2 InitialSize => new Vector2(750f, 550f);

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
                    "SimpleTrans.NoChangesConfirm".Translate(pawn.Named("PAWN"), cycleLabel).Resolve(),
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
            var titleText = "SimpleTrans.ConsentDialogTitle".Translate(cycleLabel).Resolve();
            float titleHeight = Text.CalcHeight(titleText, inRect.width);
            Widgets.Label(new Rect(0f, y, inRect.width, titleHeight), titleText);
            y += titleHeight + 5f; // Add small buffer after title

            // Subtitle with pawn name
            Text.Font = GameFont.Small;
            var subtitleText = "SimpleTrans.ConsentDialogSubtitle".Translate(pawn.Named("PAWN")).Resolve();
            float subtitleHeight = Text.CalcHeight(subtitleText, inRect.width);
            Widgets.Label(new Rect(0f, y, inRect.width, subtitleHeight), subtitleText);
            y += subtitleHeight + 15f; // Add larger buffer for spacing

            // Changes section
            Text.Font = GameFont.Small;
            var changesLabel = "SimpleTrans.ConsentChangesHeader".Translate().Resolve();
            Widgets.Label(new Rect(0f, y, inRect.width, 25f), changesLabel);
            y += 30f;

            // Scrollable changes list  
            Rect outRect = new Rect(inRect.x, y, inRect.width, inRect.height - 135f - y);
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

            // Warning text - start further up to accommodate both warnings
            float warningsStartY = inRect.height - 120f;
            y = warningsStartY;

            // Check if pregnancy warning is needed
            bool showPregnancyWarning = WillTerminatePregnancy(pawn, cycle);

            if (showPregnancyWarning)
            {
                // Pregnancy warning in red
                GUI.color = Color.red;
                Text.Font = GameFont.Tiny;
                var pregnancyWarningText = "SimpleTrans.ConsentPregnancyWarning".Translate(pawn.Named("PAWN")).Resolve();
                float pregnancyWarningHeight = Text.CalcHeight(pregnancyWarningText, inRect.width);
                Widgets.Label(new Rect(0f, y, inRect.width, pregnancyWarningHeight), pregnancyWarningText);
                y += pregnancyWarningHeight + 5f;
            }

            // General warning in yellow
            GUI.color = Color.yellow;
            Text.Font = GameFont.Tiny;
            var warningText = "SimpleTrans.ConsentWarning".Translate().Resolve();
            float warningHeight = Text.CalcHeight(warningText, inRect.width);
            Widgets.Label(new Rect(0f, y, inRect.width, warningHeight), warningText);
            GUI.color = oriColor;

            // Buttons
            y = inRect.height - 35f;
            if (Widgets.ButtonText(new Rect(0f, y, inRect.width / 2f - 10f, 35f), "CancelButton".Translate().Resolve()))
            {
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.width / 2f + 10f, y, inRect.width / 2f - 10f, 35f), "SimpleTrans.ConsentProceed".Translate().Resolve()))
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
                case TransformationChangeType.Information:
                    GUI.color = Color.cyan;
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
                TransformationChangeType.Information => "ℹ",
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

            // Check for pregnancy effects first
            bool willTerminatePregnancy = WillTerminatePregnancy(pawn, cycle);
            bool willPreservePregnancy = WillPreservePregnancy(pawn, cycle);

            if (willTerminatePregnancy)
            {
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.TerminatePregnancy".Translate().Resolve()));
            }
            else if (willPreservePregnancy)
            {
                changes.Add(new TransformationChange(TransformationChangeType.Information, "SimpleTrans.Change.PreservePregnancy".Translate().Resolve()));
            }

            // We need to cast to our specific cycle types to analyze changes
            switch (cycle)
            {
                case CompBiosculpterPod_GenderAffirmingCycle genderAffirmingCycle:
                    AnalyzeGenderAffirmingChanges(pawn, genderAffirmingCycle, changes);
                    break;
            }

            return changes;
        }

        /// <summary>
        /// Determines if a transformation will terminate pregnancy
        /// </summary>
        private static bool WillTerminatePregnancy(Pawn pawn, CompBiosculpterPod_Cycle cycle)
        {
            // Only matters if pawn is pregnant and has carry ability
            if (RimWorld.PregnancyUtility.GetPregnancyHediff(pawn) == null || !SimpleTransPregnancyUtility.CanCarry(pawn))
                return false;

            // Only terminate pregnancy when REMOVING carry ability (the metaphorical uterus)
            switch (cycle)
            {
                case CompBiosculpterPod_GenderAffirmingCycle genderAffirmingCycle:
                    var choices = genderAffirmingCycle.GetChoices();
                    if (choices == null) return false;
                    // Terminate if removing carry ability
                    return choices.ReproductiveCapability == ReproductiveCapability.None ||
                           choices.ReproductiveCapability == ReproductiveCapability.SireOnly;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines if a transformation will preserve pregnancy (show positive message)
        /// </summary>
        private static bool WillPreservePregnancy(Pawn pawn, CompBiosculpterPod_Cycle cycle)
        {
            // Only show preserve message if pawn is actually pregnant
            if (RimWorld.PregnancyUtility.GetPregnancyHediff(pawn) == null)
                return false;

            // Show preserve message for cycles that keep/add carry ability
            switch (cycle)
            {
                case CompBiosculpterPod_GenderAffirmingCycle genderAffirmingCycle:
                    var choices = genderAffirmingCycle.GetChoices();
                    if (choices == null) return false;
                    // Preserve if keeping or adding carry ability
                    return choices.ReproductiveCapability == ReproductiveCapability.CarryOnly ||
                           choices.ReproductiveCapability == ReproductiveCapability.Both;
                default:
                    return false;
            }
        }

        private static void AnalyzeGenderAffirmingChanges(Pawn pawn, CompBiosculpterPod_GenderAffirmingCycle cycle, List<TransformationChange> changes)
        {
            var choices = cycle.GetChoices();
            if (choices == null)
            {
                changes.Add(new TransformationChange(TransformationChangeType.Information, "SimpleTrans.Change.NoChoicesSelected".Translate().Resolve()));
                return;
            }


            // Body type changes
            if (choices.BodyType != pawn.story?.bodyType)
            {
                changes.Add(new TransformationChange(TransformationChangeType.Modification,
                    "SimpleTrans.Change.SetBodyType".Translate(choices.BodyType.defName.CapitalizeFirst()).Resolve()));
            }


            // Reproductive capability changes
            var currentCapability = GetCurrentReproductiveCapability(pawn);
            if (choices.ReproductiveCapability != currentCapability)
            {
                AnalyzeReproductiveCapabilityChanges(pawn, currentCapability, choices.ReproductiveCapability, changes);
            }

            // Prosthetics removal (Gender Affirming Cycle removes all prosthetics like other cycles)
            AnalyzeProstheticsRemoval(pawn, changes);
        }

        private static void AnalyzeReproductiveCapabilityChanges(Pawn pawn, ReproductiveCapability current, ReproductiveCapability target, List<TransformationChange> changes)
        {
            bool currentCarry = current == ReproductiveCapability.CarryOnly || current == ReproductiveCapability.Both;
            bool currentSire = current == ReproductiveCapability.SireOnly || current == ReproductiveCapability.Both;
            bool targetCarry = target == ReproductiveCapability.CarryOnly || target == ReproductiveCapability.Both;
            bool targetSire = target == ReproductiveCapability.SireOnly || target == ReproductiveCapability.Both;

            // Check carry ability changes
            if (currentCarry && !targetCarry)
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.RemoveCarryingAbility".Translate().Resolve()));
            else if (!currentCarry && targetCarry)
                changes.Add(new TransformationChange(TransformationChangeType.Addition, "SimpleTrans.Change.AddCarryingAbility".Translate().Resolve()));

            // Check sire ability changes
            if (currentSire && !targetSire)
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.RemoveSiringAbility".Translate().Resolve()));
            else if (!currentSire && targetSire)
                changes.Add(new TransformationChange(TransformationChangeType.Addition, "SimpleTrans.Change.AddSiringAbility".Translate().Resolve()));
        }

        private static GenderIdentity GetCurrentIdentity(Pawn pawn)
        {
            if (pawn.health.hediffSet.HasHediff(SimpleTransPregnancyUtility.transDef))
                return GenderIdentity.Transgender;
            else if (pawn.health.hediffSet.HasHediff(SimpleTransPregnancyUtility.cisDef))
                return GenderIdentity.Cisgender;
            else
                return GenderIdentity.Cisgender; // Default
        }

        private static ReproductiveCapability GetCurrentReproductiveCapability(Pawn pawn)
        {
            bool canCarry = SimpleTransPregnancyUtility.CanCarry(pawn);
            bool canSire = SimpleTransPregnancyUtility.CanSire(pawn);

            if (canCarry && canSire)
                return ReproductiveCapability.Both;
            else if (canCarry)
                return ReproductiveCapability.CarryOnly;
            else if (canSire)
                return ReproductiveCapability.SireOnly;
            else
                return ReproductiveCapability.None;
        }

        private static void AnalyzeProstheticsRemoval(Pawn pawn, List<TransformationChange> changes)
        {
            var hediffs = pawn.health.hediffSet.hediffs;

            if (hediffs.Any(h => h.def.defName == "BasicProstheticCarry"))
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.RemoveBasicCarryProsthetic".Translate().Resolve()));

            if (hediffs.Any(h => h.def.defName == "BasicProstheticSire"))
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.RemoveBasicSireProsthetic".Translate().Resolve()));

            if (hediffs.Any(h => h.def.defName == "BionicProstheticCarry"))
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.RemoveBionicCarryProsthetic".Translate().Resolve()));

            if (hediffs.Any(h => h.def.defName == "BionicProstheticSire"))
                changes.Add(new TransformationChange(TransformationChangeType.Removal, "SimpleTrans.Change.RemoveBionicSireProsthetic".Translate().Resolve()));
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
