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
    /// Choice dialog for Gender Affirming Cycle - allows users to select transformation parameters
    /// </summary>
    public class SimpleTransDialog_GenderAffirmingChoice : Window
    {
        private readonly Pawn pawn;
        private readonly CompBiosculpterPod_GenderAffirmingCycle cycle;
        private readonly Action proceedAction;

        private GenderAffirmingChoices choices;

        private Vector2 scrollPosition = Vector2.zero;
        private int currentTab = 0;
        private readonly string[] tabLabels = { "BodyType", "Reproduction" };

        public override Vector2 InitialSize => new Vector2(600f, 500f);

        public static void CreateDialog(Pawn pawn, CompBiosculpterPod_GenderAffirmingCycle cycle, Action proceedAction)
        {
            if (pawn == null)
            {
                Log.Error($"Attempted creating {nameof(SimpleTransDialog_GenderAffirmingChoice)} with null pawn");
                return;
            }

            Find.WindowStack.Add(new SimpleTransDialog_GenderAffirmingChoice(pawn, cycle, proceedAction));
        }

        private SimpleTransDialog_GenderAffirmingChoice(Pawn pawn, CompBiosculpterPod_GenderAffirmingCycle cycle, Action proceedAction)
        {
            this.pawn = pawn;
            this.cycle = cycle;
            this.proceedAction = proceedAction;

            // Initialize choices with current state
            choices = new GenderAffirmingChoices
            {
                BodyType = pawn.story?.bodyType ?? BodyTypeDefOf.Male,
                ReproductiveCapability = GetCurrentReproductiveCapability(pawn)
            };

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
            var titleText = "SimpleTrans.GenderAffirmingChoice.Title".Translate().Resolve();
            float titleHeight = Text.CalcHeight(titleText, inRect.width);
            Widgets.Label(new Rect(0f, y, inRect.width, titleHeight), titleText);
            y += titleHeight + 5f;

            // Subtitle with pawn name
            Text.Font = GameFont.Small;
            var subtitleText = "SimpleTrans.GenderAffirmingChoice.Subtitle".Translate(pawn.Named("PAWN")).Resolve();
            float subtitleHeight = Text.CalcHeight(subtitleText, inRect.width);
            Widgets.Label(new Rect(0f, y, inRect.width, subtitleHeight), subtitleText);
            y += subtitleHeight + 15f;

            // Tab buttons
            float tabWidth = inRect.width / tabLabels.Length;
            for (int i = 0; i < tabLabels.Length; i++)
            {
                Rect tabRect = new Rect(i * tabWidth, y, tabWidth, 30f);
                bool isSelected = currentTab == i;

                if (isSelected)
                    GUI.color = Color.yellow;

                if (Widgets.ButtonText(tabRect, tabLabels[i].Translate().Resolve()))
                {
                    currentTab = i;
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                }

                GUI.color = oriColor;
            }
            y += 35f;

            // Tab content area
            Rect contentRect = new Rect(0f, y, inRect.width, inRect.height - y - 80f);
            DrawTabContent(contentRect);

            // Buttons
            float buttonY = inRect.height - 40f;
            if (Widgets.ButtonText(new Rect(0f, buttonY, inRect.width / 2f - 10f, 35f), "CancelButton".Translate().Resolve()))
            {
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.width / 2f + 10f, buttonY, inRect.width / 2f - 10f, 35f), "SimpleTrans.GenderAffirmingChoice.Continue".Translate().Resolve()))
            {
                Close();
                // Apply choices to cycle and proceed to consent dialog
                cycle.SetChoices(choices);
                SimpleTransDialog_TransformationConsent.CreateDialog(pawn, cycle, "SimpleTrans.GenderAffirmingChoice.CycleLabel".Translate().Resolve(), proceedAction);
            }

            Text.Font = oriFont;
            GUI.color = oriColor;
        }

        private void DrawTabContent(Rect rect)
        {
            switch (currentTab)
            {
                case 0:
                    DrawBodyTypeTab(rect);
                    break;
                case 1:
                    DrawReproductionTab(rect);
                    break;
            }
        }

        private void DrawBodyTypeTab(Rect rect)
        {
            float y = rect.y + 10f;
            var bodyTypes = new List<BodyTypeDef>
            {
                BodyTypeDefOf.Male,
                BodyTypeDefOf.Female,
                BodyTypeDefOf.Thin,
                BodyTypeDefOf.Fat,
                BodyTypeDefOf.Hulk
            };

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, y, rect.width, 25f), "SimpleTrans.GenderAffirmingChoice.CurrentBodyType".Translate(pawn.story.bodyType.defName ?? "None").Resolve());
            y += 30f;

            foreach (var bodyType in bodyTypes)
            {
                Rect optionRect = new Rect(rect.x + 20f, y, rect.width - 40f, 25f);
                bool isSelected = choices.BodyType == bodyType;

                if (DrawRadioOption(optionRect, bodyType.defName.CapitalizeFirst(), isSelected))
                {
                    choices.BodyType = bodyType;
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                }

                y += 30f;
            }
        }


        private void DrawReproductionTab(Rect rect)
        {
            float y = rect.y + 10f;
            var capabilities = new List<ReproductiveCapability>
            {
                ReproductiveCapability.None,
                ReproductiveCapability.CarryOnly,
                ReproductiveCapability.SireOnly,
                ReproductiveCapability.Both
            };

            Text.Font = GameFont.Small;
            var currentCapability = GetCurrentReproductiveCapability(pawn);
            Widgets.Label(new Rect(rect.x, y, rect.width, 25f), "SimpleTrans.GenderAffirmingChoice.CurrentReproduction".Translate(GetCapabilityLabel(currentCapability)).Resolve());
            y += 30f;

            foreach (var capability in capabilities)
            {
                Rect optionRect = new Rect(rect.x + 20f, y, rect.width - 40f, 25f);
                bool isSelected = choices.ReproductiveCapability == capability;

                if (DrawRadioOption(optionRect, GetCapabilityLabel(capability), isSelected))
                {
                    choices.ReproductiveCapability = capability;
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                }

                y += 30f;
            }
        }

        private bool DrawRadioOption(Rect rect, string label, bool selected)
        {
            bool clicked = Widgets.ButtonInvisible(rect);

            Widgets.RadioButton(rect.x, rect.y + rect.height / 2f - 12f, selected);

            Widgets.Label(new Rect(rect.x + 30f, rect.y, rect.width - 30f, rect.height), label);

            return clicked && !selected;
        }


        private ReproductiveCapability GetCurrentReproductiveCapability(Pawn pawn)
        {
            bool canCarry = SimpleTransHediffs.CanCarry(pawn);
            bool canSire = SimpleTransHediffs.CanSire(pawn);

            if (canCarry && canSire)
                return ReproductiveCapability.Both;
            else if (canCarry)
                return ReproductiveCapability.CarryOnly;
            else if (canSire)
                return ReproductiveCapability.SireOnly;
            else
                return ReproductiveCapability.None;
        }


        private string GetCapabilityLabel(ReproductiveCapability capability)
        {
            return capability switch
            {
                ReproductiveCapability.None => "SimpleTrans.Reproduction.None".Translate().Resolve(),
                ReproductiveCapability.CarryOnly => "SimpleTrans.Reproduction.CarryOnly".Translate().Resolve(),
                ReproductiveCapability.SireOnly => "SimpleTrans.Reproduction.SireOnly".Translate().Resolve(),
                ReproductiveCapability.Both => "SimpleTrans.Reproduction.Both".Translate().Resolve(),
                _ => "Unknown"
            };
        }
    }

    /// <summary>
    /// Data structure to hold all gender affirming choices
    /// </summary>
    public class GenderAffirmingChoices
    {
        public BodyTypeDef BodyType;
        public ReproductiveCapability ReproductiveCapability;
    }


    public enum ReproductiveCapability
    {
        None,
        CarryOnly,
        SireOnly,
        Both
    }
}
