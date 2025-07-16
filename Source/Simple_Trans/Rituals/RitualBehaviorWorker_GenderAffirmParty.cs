using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Simple_Trans
{
    public class RitualBehaviorWorker_GenderAffirmParty : RitualBehaviorWorker
    {
        public RitualBehaviorWorker_GenderAffirmParty()
        {
        }

        public RitualBehaviorWorker_GenderAffirmParty(RitualBehaviorDef def) : base(def)
        {
        }

        public override string CanStartRitualNow(TargetInfo target, Precept_Ritual ritual, Pawn selectedPawn = null, Dictionary<string, Pawn> forcedForRole = null)
        {
            // Check if there are colonists available to be the celebrant
            if (target.Map != null && target.Map.mapPawns.FreeColonistsSpawned.Any())
            {
                // Let the base method handle standard validation (including role assignment)
                return base.CanStartRitualNow(target, ritual, selectedPawn, forcedForRole);
            }

            return "No colonists available to celebrate.";
        }
    }
}
