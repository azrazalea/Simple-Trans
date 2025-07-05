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
            baseDesc += "\n\n<color=green>Fertility restoration treatment:</color>";
            baseDesc += "\n• Fixes sterility and fertility issues";
            baseDesc += "\n• Restores natural reproductive abilities based on gender identity";
            baseDesc += "\n• Does not change gender, body type, or gender identity";
            baseDesc += "\n• Preserves existing prosthetics and enhancements";
            
            return baseDesc;
        }

        public override void CycleCompleted(Pawn pawn)
        {
            // Simple fertility restoration - remove sterility and restore natural abilities
            // This is much simpler than the other cycles - no full reset, no visual changes
            
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
            
            // Only restore abilities if pawn has clear gender identity, preserve existing setup
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
            // For non-binary or unclear cases, leave reproductive abilities as-is
            
            // No visual changes needed - this is purely internal fertility restoration
            
            Messages.Message("SimpleTransFertilityRestored".Translate(pawn.Named("PAWN")), 
                pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}