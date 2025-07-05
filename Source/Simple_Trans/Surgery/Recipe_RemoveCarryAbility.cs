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
            return SimpleTransPregnancyUtility.CanCarry(pawn);
        }
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            Log.Message($"[Simple Trans] RemoveCarryAbility.ApplyOnPawn called for {pawn.Name}");
            
            if (!SimpleTransPregnancyUtility.CanCarry(pawn))
            {
                Log.Warning($"[Simple Trans] Pawn does not have carry ability to remove!");
                return;
            }

            // Determine what type of part to spawn back based on current hediff
            var carryHediff = GetCarryHediff(pawn);
            if (carryHediff == null)
            {
                Log.Warning($"[Simple Trans] No carry hediff found to remove!");
                return;
            }

            var itemToSpawn = GetItemForCurrentCarryType(pawn);
            Thing extractedItem = ThingMaker.MakeThing(ThingDef.Named(itemToSpawn));
            
            // Remove all carry-related hediffs (base + any prosthetic modifiers)
            var hediffsToRemove = new List<Hediff>();
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.def.defName == "PregnancyCarry" ||
                    hediff.def.defName == "BasicProstheticCarry" ||
                    hediff.def.defName == "BionicProstheticCarry")
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

            Log.Message($"[Simple Trans] After surgery - CanCarry: {SimpleTransPregnancyUtility.CanCarry(pawn)}, CanSire: {SimpleTransPregnancyUtility.CanSire(pawn)}");
            
            Messages.Message("SimpleTransOrganExtracted".Translate(pawn.Named("PAWN"), extractedItem.Label), 
                pawn, MessageTypeDefOf.NeutralEvent);
        }

        private Hediff GetCarryHediff(Pawn pawn)
        {
            // Always return the base carry hediff - we'll handle prosthetics separately
            return pawn.health.hediffSet.GetFirstHediffOfDef(SimpleTransPregnancyUtility.canCarryDef);
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