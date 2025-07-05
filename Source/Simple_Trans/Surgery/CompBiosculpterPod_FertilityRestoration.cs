using System.Linq;
using RimWorld;
using Verse;

namespace Simple_Trans
{
    public class CompBiosculpterPod_FertilityRestoration : CompBiosculpterPod_Cycle
    {
        public override string Description(Pawn tunedFor)
        {
            if (tunedFor == null) return Props.description;
            
            string baseDesc = Props.description;
            
            // Add comprehensive description of what the cycle does
            baseDesc += "\n\n<color=green>" + "SimpleTrans.CycleDesc.FertilityRestoration".Translate() + "</color>";
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.FixesSterility".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.RestoresNaturalAbilities".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.NoGenderChange".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.PreservesProsthetics".Translate();
            
            return baseDesc;
        }

        public override void CycleCompleted(Pawn pawn)
        {
            // Simple fertility restoration - remove sterility and restore natural abilities
            // This is much simpler than the other cycles - no full reset, no visual changes
            // For pregnant females, we preserve the pregnancy
            
            // Remove sterility-related hediffs
            var sterilityHediffs = pawn.health.hediffSet.hediffs.Where(h => 
                h.def.defName == "Sterilized" || 
                h.def.defName == "SterilityPermanent" ||
                h.def.defName.Contains("Sterility") ||
                h.def.defName.Contains("Infertility")).ToList();
            
            foreach (var hediff in sterilityHediffs)
            {
                pawn.health.RemoveHediff(hediff);
            }
            
            // Restore natural reproductive abilities based on current gender
            // Don't use ValidateOrSetGender as that would randomize - manually set based on current state
            bool isTrans = pawn.health.hediffSet.HasHediff(SimpleTransPregnancyUtility.transDef);
            bool isCis = pawn.health.hediffSet.HasHediff(SimpleTransPregnancyUtility.cisDef);
            
            // Only restore abilities for binary genders with clear identity
            // For non-binary pawns, we don't assume what reproductive abilities they want
            if (pawn.gender == Gender.Male && isTrans)
            {
                // Trans male - should have carry ability
                if (!SimpleTransPregnancyUtility.CanCarry(pawn))
                    SimpleTransPregnancyUtility.SetCarry(pawn, false);
            }
            else if (pawn.gender == Gender.Male && isCis)
            {
                // Cis male - should have sire ability  
                if (!SimpleTransPregnancyUtility.CanSire(pawn))
                    SimpleTransPregnancyUtility.SetSire(pawn, false);
            }
            else if (pawn.gender == Gender.Female && isTrans)
            {
                // Trans female - should have sire ability
                if (!SimpleTransPregnancyUtility.CanSire(pawn))
                    SimpleTransPregnancyUtility.SetSire(pawn, false);
            }
            else if (pawn.gender == Gender.Female && isCis)
            {
                // Cis female - should have carry ability
                if (!SimpleTransPregnancyUtility.CanCarry(pawn))
                    SimpleTransPregnancyUtility.SetCarry(pawn, false);
            }
            // For non-binary pawns, only remove sterility - don't modify reproductive abilities
            
            // No visual changes needed - this is purely internal fertility restoration
            
            Messages.Message("SimpleTransFertilityRestored".Translate(pawn.Named("PAWN")), 
                pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}