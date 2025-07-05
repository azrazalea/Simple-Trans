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
            baseDesc += "\n\n<color=cyan>" + "SimpleTrans.CycleDesc.MasculinizingTransformation".Translate() + "</color>";
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.SetsGenderMale".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.ChangeBodyMale".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.GrantSireRemoveCarry".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.RemoveAllProsthetics".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.SetCisgender".Translate();
            
            return baseDesc;
        }

        public override void CycleCompleted(Pawn pawn)
        {
            // Handle pregnancy termination if pawn had carry ability
            if (SimpleTransPregnancyUtility.CanCarry(pawn) && RimWorld.PregnancyUtility.GetPregnancyHediff(pawn) != null && RimWorld.PregnancyUtility.TryTerminatePregnancy(pawn) && PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                Messages.Message("MessagePregnancyTerminated".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
            }
            
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
            baseDesc += "\n\n<color=#FF69B4>" + "SimpleTrans.CycleDesc.FeminizingTransformation".Translate() + "</color>";
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.SetsGenderFemale".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.ChangeBodyFemale".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.GrantCarryRemoveSire".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.RemoveAllProsthetics".Translate();
            baseDesc += "\n• " + "SimpleTrans.CycleDesc.SetCisgender".Translate();
            
            return baseDesc;
        }

        public override void CycleCompleted(Pawn pawn)
        {
            // Feminizing preserves pregnancy - we're adding/keeping carry ability
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
