using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Simple_Trans
{
    public class Recipe_RemoveCarryAbility : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!(thing is Pawn pawn)) return false;
            // Only available if pawn has carry ability to remove
            return SimpleTransHediffs.CanCarry(pawn);
        }
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            Log.Message($"[Simple Trans] RemoveCarryAbility.ApplyOnPawn called for {pawn.Name}");
            
            if (!SimpleTransHediffs.CanCarry(pawn))
            {
                Log.Warning($"[Simple Trans] Pawn does not have carry ability to remove!");
                return;
            }

            if (billDoer != null && !CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                // Determine what type of part to spawn back based on current hediff
                var carryHediff = GetCarryHediff(pawn);
                if (carryHediff == null)
                {
                    Log.Warning($"[Simple Trans] No carry hediff found to remove!");
                    return;
                }

                var itemToSpawn = GetItemForCurrentCarryType(pawn);
                Thing extractedItem = ThingMaker.MakeThing(ThingDef.Named(itemToSpawn));
                
                // Handle pregnancy termination before removing carry ability
                if (RimWorld.PregnancyUtility.GetPregnancyHediff(pawn) != null && RimWorld.PregnancyUtility.TryTerminatePregnancy(pawn) && PawnUtility.ShouldSendNotificationAbout(pawn))
                {
                    Messages.Message("MessagePregnancyTerminated".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
                    if (IsViolationOnPawn(pawn, part, Faction.OfPlayerSilentFail))
                    {
                        ReportViolation(pawn, billDoer, pawn.HomeFaction, -70);
                    }
                }
                
                // Remove all carry-related hediffs (base + any prosthetic modifiers + sterilization)
                var hediffsToRemove = new List<Hediff>();
                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    if (hediff.def.defName == "PregnancyCarry" ||
                        hediff.def.defName == "BasicProstheticCarry" ||
                        hediff.def.defName == "BionicProstheticCarry" ||
                        hediff.def.defName == "SterilizedCarry" ||
                        hediff.def.defName == "ReversibleSterilizedCarry" ||
                        hediff.def.defName == "Sterilized")
                    {
                        hediffsToRemove.Add(hediff);
                    }
                }
                
                foreach (var hediff in hediffsToRemove)
                {
                    pawn.health.RemoveHediff(hediff);
                }
                
                // Spawn the extracted item
                GenPlace.TryPlaceThing(extractedItem, pawn.Position, pawn.Map, ThingPlaceMode.Near);

                // Record tale for surgery
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);

                Log.Message($"[Simple Trans] After surgery - CanCarry: {SimpleTransHediffs.CanCarry(pawn)}, CanSire: {SimpleTransHediffs.CanSire(pawn)}");
                
                Messages.Message("SimpleTransOrganExtracted".Translate(pawn.Named("PAWN"), extractedItem.Label), 
                    pawn, MessageTypeDefOf.NeutralEvent);
            }
        }

        private Hediff GetCarryHediff(Pawn pawn)
        {
            // Always return the base carry hediff - we'll handle prosthetics separately
            return pawn.health.hediffSet.GetFirstHediffOfDef(SimpleTransHediffs.canCarryDef);
        }

        private string GetItemForCurrentCarryType(Pawn pawn)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            
            // Check for prosthetic modifiers first
            foreach (var hediff in hediffs)
            {
                if (hediff.def.defName == "BionicProstheticCarry")
                    return "BionicReproductiveProsthetic_Carry";
                if (hediff.def.defName == "BasicProstheticCarry")
                    return "BasicReproductiveProsthetic_Carry";
            }
            
            // Default to natural organs
            return "CarryingOrgans";
        }
    }
}