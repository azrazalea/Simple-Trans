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
        }
    }
}
