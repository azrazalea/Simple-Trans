using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Simple_Trans
{
    public class CompBiosculpterPod_Androgynize : CompBiosculpterPod_Cycle
    {
        public override string Description(Pawn tunedFor)
        {
            if (tunedFor == null) return Props.description;
            
            string baseDesc = Props.description;
            
            // Add comprehensive description of what the cycle does
            baseDesc += "\n\n<color=purple>Complete androgynizing transformation:</color>";
            
            // Check if NonBinary Gender mod is loaded
            bool hasNBGMod = ModsConfig.IsActive("Coraizon.NonBinaryGenderMod") || ModsConfig.IsActive("Coraizon.NBGM");
            
            if (hasNBGMod)
                baseDesc += "\n• Sets gender to non-binary";
            else
                baseDesc += "\n• Gender remains unchanged (non-binary mod not loaded)";
            baseDesc += "\n• Changes body type to thin";
            baseDesc += "\n• Removes all reproductive abilities";
            baseDesc += "\n• Removes all reproductive prosthetics";
            baseDesc += "\n• Sets gender identity to transgender";
                
            return baseDesc;
        }

        public override void CycleCompleted(Pawn pawn)
        {
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
            
            // NO reproductive abilities - this is the androgynous result
            // (ClearGender already removed them all)
            
            // Set to transgender
            SimpleTransPregnancyUtility.SetTrans(pawn);
            
            // Set to thin body type
            pawn.story.bodyType = BodyTypeDefOf.Thin;
            
            // Refresh graphics
            pawn.Drawer.renderer.SetAllGraphicsDirty();
            
            Messages.Message("SimpleTransAndrogynized".Translate(pawn.Named("PAWN")), 
                pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}
