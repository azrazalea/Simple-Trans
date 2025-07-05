using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Simple_Trans
{
    public class CompBiosculpterPod_ReproductiveReconstructionMasculinizing : CompBiosculpterPod_Cycle
    {
        public override string Description(Pawn tunedFor)
        {
            if (tunedFor == null) return Props.description;
            
            string baseDesc = Props.description;
            
            // Add comprehensive description of what the cycle does
            baseDesc += "\n\n<color=cyan>Complete masculinizing transformation:</color>";
            baseDesc += "\n• Sets gender to male";
            baseDesc += "\n• Changes body type to male";
            baseDesc += "\n• Grants sire ability, removes carry ability";
            baseDesc += "\n• Removes all reproductive prosthetics";
            baseDesc += "\n• Sets gender identity to cisgender";
            
            return baseDesc;
        }

        public override void CycleCompleted(Pawn pawn)
        {
            // Complete reset - clear all gender and reproductive hediffs
            SimpleTransPregnancyUtility.ClearGender(pawn);
            
            // Set to male
            pawn.gender = Gender.Male;
            pawn.story.bodyType = BodyTypeDefOf.Male;
            
            // Set reproductive ability
            SimpleTransPregnancyUtility.SetSire(pawn, false);
            
            // Set to cisgender
            SimpleTransPregnancyUtility.SetCis(pawn);
            
            // Refresh graphics
            pawn.Drawer.renderer.SetAllGraphicsDirty();
            
            Messages.Message("SimpleTransMasculinizingGranted".Translate(pawn.Named("PAWN")), 
                pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
    
    public class CompBiosculpterPod_ReproductiveReconstructionFeminizing : CompBiosculpterPod_Cycle
    {
        public override string Description(Pawn tunedFor)
        {
            if (tunedFor == null) return Props.description;
            
            string baseDesc = Props.description;
            
            // Add comprehensive description of what the cycle does
            baseDesc += "\n\n<color=#FF69B4>Complete feminizing transformation:</color>";
            baseDesc += "\n• Sets gender to female";
            baseDesc += "\n• Changes body type to female";
            baseDesc += "\n• Grants carry ability, removes sire ability";
            baseDesc += "\n• Removes all reproductive prosthetics";
            baseDesc += "\n• Sets gender identity to cisgender";
            
            return baseDesc;
        }

        public override void CycleCompleted(Pawn pawn)
        {
            // Complete reset - clear all gender and reproductive hediffs
            SimpleTransPregnancyUtility.ClearGender(pawn);
            
            // Set to female
            pawn.gender = Gender.Female;
            pawn.story.bodyType = BodyTypeDefOf.Female;
            
            // Set reproductive ability
            SimpleTransPregnancyUtility.SetCarry(pawn, false);
            
            // Set to cisgender
            SimpleTransPregnancyUtility.SetCis(pawn);
            
            // Refresh graphics
            pawn.Drawer.renderer.SetAllGraphicsDirty();
            
            Messages.Message("SimpleTransFeminizingGranted".Translate(pawn.Named("PAWN")), 
                pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}
