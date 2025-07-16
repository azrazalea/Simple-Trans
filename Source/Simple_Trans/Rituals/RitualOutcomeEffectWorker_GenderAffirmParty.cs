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
    }
}
