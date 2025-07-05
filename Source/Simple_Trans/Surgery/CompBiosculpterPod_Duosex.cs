using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Simple_Trans
{
    public class CompBiosculpterPod_Duosex : CompBiosculpterPod_Cycle
    {
        public override string Description(Pawn tunedFor)
        {
            if (tunedFor == null) return Props.description;
            
            string baseDesc = Props.description;
            
            // Add comprehensive description of what the cycle does
            baseDesc += "\n\n<color=magenta>" + "SimpleTrans.CycleDesc.DuosexTransformation".Translate() + "</color>";
            
            // Check if NonBinary Gender mod is loaded
            bool hasNBGMod = ModsConfig.IsActive("Coraizon.NonBinaryGenderMod") || ModsConfig.IsActive("Coraizon.NBGM");
            
            if (hasNBGMod)
                baseDesc += "\n• " + "SimpleTrans.CycleDesc.SetsGenderNonBinary".Translate();
            else
                baseDesc += "\n• " + "SimpleTrans.CycleDesc.GenderUnchanged".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.ChangeBodyThin".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.GrantBothAbilities".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.RemoveAllProsthetics".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.SetTransgender".Translate();
                
            return baseDesc;
        }

        public override void CycleCompleted(Pawn pawn)
        {
            // Duosex preserves pregnancy - we're adding both abilities including carry
            // Complete reset - clear all gender and reproductive hediffs
            SimpleTransPregnancyUtility.ClearGender(pawn);
            
            // Set gender to non-binary if NBG mod is loaded
            if (ModsConfig.IsActive("Coraizon.NonBinaryGenderMod") || ModsConfig.IsActive("Coraizon.NBGM"))
            {
                // Try to set to non-binary gender if the mod is available
                try
                {
                    pawn.gender = (Gender)3; // NonBinary is usually enum value 3
                }
                catch
                {
                    // If that fails, leave gender unchanged
                }
            }
            
            // Grant BOTH reproductive abilities - this is the duosex result
            SimpleTransPregnancyUtility.SetCarry(pawn, false);
            SimpleTransPregnancyUtility.SetSire(pawn, false);
            
            // Set to transgender
            SimpleTransPregnancyUtility.SetTrans(pawn);
            
            // Set to thin body type
            pawn.story.bodyType = BodyTypeDefOf.Thin;
            
            // Refresh graphics
            pawn.Drawer.renderer.SetAllGraphicsDirty();
            
            Messages.Message("SimpleTransDuosexComplete".Translate(pawn.Named("PAWN")), 
                pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}
