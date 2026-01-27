using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Simple_Trans
{
    /// <summary>
    /// Unified biosculpter cycle that applies transformations based on user choices
    /// </summary>
    public class CompBiosculpterPod_GenderAffirmingCycle : CompBiosculpterPod_Cycle
    {
        private GenderAffirmingChoices selectedChoices;

        public void SetChoices(GenderAffirmingChoices choices)
        {
            selectedChoices = choices;
        }

        public GenderAffirmingChoices GetChoices()
        {
            return selectedChoices;
        }

        public override string Description(Pawn tunedFor)
        {
            if (tunedFor == null) return Props.description;

            string baseDesc = Props.description;

            if (selectedChoices != null)
            {
                baseDesc += "\n\n<color=cyan>" + "SimpleTrans.GenderAffirmingChoice.SelectedChanges".Translate().Resolve() + "</color>";

                // Show selected changes
                if (selectedChoices.BodyType != tunedFor.story?.bodyType)
                    baseDesc += "\n• " + "SimpleTrans.GenderAffirmingChoice.WillChangeBodyType".Translate(selectedChoices.BodyType.label.CapitalizeFirst()).Resolve();


                var currentReproduction = GetCurrentReproductiveCapability(tunedFor);
                if (selectedChoices.ReproductiveCapability != currentReproduction)
                    baseDesc += "\n• " + "SimpleTrans.GenderAffirmingChoice.WillChangeReproduction".Translate(GetCapabilityLabel(selectedChoices.ReproductiveCapability)).Resolve();
            }
            else
            {
                baseDesc += "\n\n<color=yellow>" + "SimpleTrans.GenderAffirmingChoice.NoChoicesSelected".Translate().Resolve() + "</color>";
            }

            return baseDesc;
        }

        public override void CycleCompleted(Pawn pawn)
        {
            if (selectedChoices == null)
            {
                Log.Error($"[Simple Trans] Gender Affirming Cycle completed for {pawn?.Name?.ToStringShort ?? "unknown"} but no choices were selected!");
                return;
            }

            // Handle pregnancy termination if removing carry ability
            bool willRemoveCarry = SimpleTransHediffs.CanCarry(pawn) &&
                                   (selectedChoices.ReproductiveCapability == ReproductiveCapability.None ||
                                    selectedChoices.ReproductiveCapability == ReproductiveCapability.SireOnly);

            if (willRemoveCarry && RimWorld.PregnancyUtility.GetPregnancyHediff(pawn) != null)
            {
                if (RimWorld.PregnancyUtility.TryTerminatePregnancy(pawn) && PawnUtility.ShouldSendNotificationAbout(pawn))
                {
                    Messages.Message("MessagePregnancyTerminated".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
                }
            }

            // Apply transformations
            ApplyBodyTypeTransformation(pawn);
            ApplyReproductiveTransformation(pawn);

            // Refresh graphics
            pawn.Drawer.renderer.SetAllGraphicsDirty();

            // Success message
            Messages.Message("SimpleTrans.GenderAffirmingChoice.TransformationComplete".Translate(pawn.Named("PAWN")).Resolve(),
                pawn, MessageTypeDefOf.PositiveEvent);
        }


        private void ApplyBodyTypeTransformation(Pawn pawn)
        {
            if (selectedChoices.BodyType != pawn.story?.bodyType)
            {
                pawn.story.bodyType = selectedChoices.BodyType;

                SimpleTransDebug.Log($"Gender affirming cycle: Changed {pawn.Name?.ToStringShort ?? "unknown"}'s body type to {selectedChoices.BodyType.label}", 1);
            }
        }


        private void ApplyReproductiveTransformation(Pawn pawn)
        {
            var currentCapability = GetCurrentReproductiveCapability(pawn);
            if (selectedChoices.ReproductiveCapability != currentCapability)
            {
                // Clear existing reproductive hediffs and prosthetics, but preserve gender identity
                GenderAssignment.ClearGender(pawn, clearIdentity: false, clearCapabilities: true);

                // Apply new reproductive capabilities
                switch (selectedChoices.ReproductiveCapability)
                {
                    case ReproductiveCapability.None:
                        // No action needed - ClearGender already removed everything
                        break;
                    case ReproductiveCapability.CarryOnly:
                        GenderAssignment.SetCarry(pawn, false);
                        break;
                    case ReproductiveCapability.SireOnly:
                        GenderAssignment.SetSire(pawn, false);
                        break;
                    case ReproductiveCapability.Both:
                        GenderAssignment.SetCarry(pawn, false);
                        GenderAssignment.SetSire(pawn, false);
                        break;
                }

                SimpleTransDebug.Log($"Gender affirming cycle: Changed {pawn.Name?.ToStringShort ?? "unknown"}'s reproductive capability to {selectedChoices.ReproductiveCapability}", 1);
            }
        }

        private GenderIdentity GetCurrentIdentity(Pawn pawn)
        {
            if (pawn.health.hediffSet.HasHediff(SimpleTransHediffs.transDef))
                return GenderIdentity.Transgender;
            else if (pawn.health.hediffSet.HasHediff(SimpleTransHediffs.cisDef))
                return GenderIdentity.Cisgender;
            else
                return GenderIdentity.Cisgender; // Default
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

        private string GetIdentityLabel(GenderIdentity identity)
        {
            return identity switch
            {
                GenderIdentity.Cisgender => "SimpleTrans.Identity.Cisgender".Translate().Resolve(),
                GenderIdentity.Transgender => "SimpleTrans.Identity.Transgender".Translate().Resolve(),
                _ => "Unknown"
            };
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

        public override void PostExposeData()
        {
            base.PostExposeData();

            // Save/load the selected choices
            if (selectedChoices == null)
                selectedChoices = new GenderAffirmingChoices();

            Scribe_Values.Look(ref selectedChoices.BodyType, "selectedBodyType", BodyTypeDefOf.Male);
            Scribe_Values.Look(ref selectedChoices.ReproductiveCapability, "selectedReproductiveCapability", ReproductiveCapability.None);
        }
    }

    /// <summary>
    /// Properties class for the Gender Affirming Cycle
    /// </summary>
    public class CompProperties_BiosculpterPod_GenderAffirmingCycle : CompProperties_BiosculpterPod_BaseCycle
    {
        public CompProperties_BiosculpterPod_GenderAffirmingCycle()
        {
            compClass = typeof(CompBiosculpterPod_GenderAffirmingCycle);
        }
    }
}
