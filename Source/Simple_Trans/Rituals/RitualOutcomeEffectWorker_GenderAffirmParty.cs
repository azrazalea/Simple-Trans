using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Simple_Trans
{
    public class RitualOutcomeEffectWorker_GenderAffirmParty : RitualOutcomeEffectWorker_FromQuality
    {
        public RitualOutcomeEffectWorker_GenderAffirmParty()
        {
        }

        public RitualOutcomeEffectWorker_GenderAffirmParty(RitualOutcomeEffectDef def) : base(def)
        {
        }

        public override bool SupportsAttachableOutcomeEffect => false;

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            // Get the celebrant from the role assignments
            Pawn celebrant = jobRitual.assignments?.FirstAssignedPawn("Celebrant");
            
            if (celebrant != null)
            {
                // Show the gender affirmation dialog
                Find.WindowStack.Add(new SimpleTransDialog_GenderAffirmationChoice(celebrant, jobRitual));
            }

            // Get the quality and outcome
            float quality = GetQuality(jobRitual, progress);
            var outcome = GetOutcome(quality, jobRitual);
            
            // Apply thoughts to all participants
            foreach (Pawn pawn in totalPresence.Keys)
            {
                if (pawn == celebrant)
                {
                    // Celebrant gets special stronger thoughts
                    GiveMemoryToPawn(pawn, DefDatabase<ThoughtDef>.GetNamed("SimpleTrans_CelebrantGenderAffirmParty"), jobRitual, outcome.positivityIndex);
                }
                else
                {
                    // Regular participants get normal thoughts
                    GiveMemoryToPawn(pawn, DefDatabase<ThoughtDef>.GetNamed("SimpleTrans_AttendedGenderAffirmParty"), jobRitual, outcome.positivityIndex);
                }
            }

            // Send outcome letter
            LookTargets lookTargets = jobRitual.selectedTarget;
            string outcomeText = outcome.description.Formatted(jobRitual.Ritual.Label).CapitalizeFirst() + "\n\n" + 
                               OutcomeQualityBreakdownDesc(quality, progress, jobRitual);
            
            // Custom mood breakdown since built-in method only looks at stage 0
            string moodText = GetMoodBreakdown(outcome, celebrant, totalPresence);
            if (!moodText.NullOrEmpty())
            {
                outcomeText = outcomeText + "\n\n" + moodText;
            }

            Find.LetterStack.ReceiveLetter(
                "OutcomeLetterLabel".Translate(outcome.label.Named("OUTCOMELABEL"), jobRitual.Ritual.Label.Named("RITUALLABEL")), 
                outcomeText, 
                outcome.Positive ? LetterDefOf.RitualOutcomePositive : LetterDefOf.RitualOutcomeNegative, 
                lookTargets, 
                null, null, null, null);
        }

        private string GetMoodBreakdown(RitualOutcomePossibility outcome, Pawn celebrant, Dictionary<Pawn, int> totalPresence)
        {
            var celebrantThoughtDef = DefDatabase<ThoughtDef>.GetNamed("SimpleTrans_CelebrantGenderAffirmParty");
            var attendeeThoughtDef = DefDatabase<ThoughtDef>.GetNamed("SimpleTrans_AttendedGenderAffirmParty");
            
            if (attendeeThoughtDef == null || outcome.positivityIndex < 0 || outcome.positivityIndex >= attendeeThoughtDef.stages.Count)
                return string.Empty;

            var attendeeStage = attendeeThoughtDef.stages[outcome.positivityIndex];
            string moodText = "";
            
            // Add celebrant mood if celebrant exists and has different thought
            if (celebrant != null && celebrantThoughtDef != null && outcome.positivityIndex < celebrantThoughtDef.stages.Count)
            {
                var celebrantStage = celebrantThoughtDef.stages[outcome.positivityIndex];
                if (celebrantStage.baseMoodEffect != 0f)
                {
                    moodText += "RitualOutcomeExtraDesc_Mood".Translate(celebrantStage.baseMoodEffect.ToStringWithSign(), celebrantThoughtDef.durationDays) + " (celebrant)";
                }
            }
            
            // Add attendee mood
            if (attendeeStage.baseMoodEffect != 0f)
            {
                if (!moodText.NullOrEmpty())
                    moodText += "\n";
                moodText += "RitualOutcomeExtraDesc_Mood".Translate(attendeeStage.baseMoodEffect.ToStringWithSign(), attendeeThoughtDef.durationDays) + " (attendees)";
            }
            
            return moodText;
        }

        private void GiveMemoryToPawn(Pawn pawn, ThoughtDef thoughtDef, LordJob_Ritual jobRitual, int positivityIndex)
        {
            if (thoughtDef != null && pawn.needs?.mood?.thoughts?.memories != null)
            {
                // positivityIndex is already 0,1,2,3 from the XML, so use it directly as stageIndex
                int stageIndex = positivityIndex;
                if (stageIndex < 0) stageIndex = 0;
                if (stageIndex >= thoughtDef.stages.Count) stageIndex = thoughtDef.stages.Count - 1;

                var thought = (Thought_Memory)ThoughtMaker.MakeThought(thoughtDef);
                thought.SetForcedStage(stageIndex);
                pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
            }
        }
    }
}