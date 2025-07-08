using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Simple_Trans
{
    public class Recipe_RemoveSireAbility : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!(thing is Pawn pawn)) return false;
            // Only available if pawn has sire ability to remove
            return SimpleTransPregnancyUtility.CanSire(pawn);
        }
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            Log.Message($"[Simple Trans] RemoveSireAbility.ApplyOnPawn called for {pawn.Name}");
            
            if (!SimpleTransPregnancyUtility.CanSire(pawn))
            {
                Log.Warning($"[Simple Trans] Pawn does not have sire ability to remove!");
                return;
            }

            if (billDoer != null && !CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                // Determine what type of part to spawn back based on current hediffs
                var itemToSpawn = GetItemForCurrentSireType(pawn);
                Thing extractedItem = ThingMaker.MakeThing(ThingDef.Named(itemToSpawn));
                
                // Remove all sire-related hediffs (base + any prosthetic modifiers + sterilization)
                var hediffsToRemove = new List<Hediff>();
                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    if (hediff.def.defName == "PregnancySire" ||
                        hediff.def.defName == "BasicProstheticSire" ||
                        hediff.def.defName == "BionicProstheticSire" ||
                        hediff.def.defName == "SterilizedSire" ||
                        hediff.def.defName == "ReversibleSterilizedSire" ||
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

                Log.Message($"[Simple Trans] After surgery - CanCarry: {SimpleTransPregnancyUtility.CanCarry(pawn)}, CanSire: {SimpleTransPregnancyUtility.CanSire(pawn)}");
                
                Messages.Message("SimpleTransOrganExtracted".Translate(pawn.Named("PAWN"), extractedItem.Label), 
                    pawn, MessageTypeDefOf.NeutralEvent);
            }
        }

        private string GetItemForCurrentSireType(Pawn pawn)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            
            // Check for prosthetic modifiers first
            foreach (var hediff in hediffs)
            {
                if (hediff.def.defName == "BionicProstheticSire")
                    return "BionicReproductiveProsthetic_Sire";
                if (hediff.def.defName == "BasicProstheticSire")
                    return "BasicReproductiveProsthetic_Sire";
            }
            
            // Default to natural organs
            return "SiringOrgans";
        }
    }
}