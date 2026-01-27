using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Simple_Trans
{
    public class Recipe_AddReproductiveAbility : Recipe_Surgery
    {
        public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord part = null)
        {
            if (!(thing is Pawn pawn)) return "SimpleTrans.Surgery.MustBePawn".Translate();

            // Try to get ingredients from the bill
            var bill = Find.Selector.SingleSelectedThing as IThingHolder;
            if (bill?.GetDirectlyHeldThings() != null)
            {
                var ingredients = bill.GetDirectlyHeldThings().ToList();
                var ingredient = GetReproductiveIngredient(ingredients);
                
                if (ingredient == null) 
                    return "SimpleTrans.Surgery.NoOrgansFound".Translate();

                var ingredientDefName = ingredient.def.defName;
                
                // Check if pawn already has the exact same type
                if (HasMatchingHediff(pawn, ingredientDefName))
                    return "SimpleTrans.Surgery.AlreadyHas".Translate(ingredient.Label.ToLower());

                // Check basic ability requirements
                bool isCarryIngredient = ingredientDefName.Contains("Carry");
                bool isSireIngredient = ingredientDefName.Contains("Sire");
                
                if (isCarryIngredient && SimpleTransHediffs.CanCarry(pawn))
                    return "SimpleTrans.Surgery.AlreadyHasCarry".Translate();
                if (isSireIngredient && SimpleTransHediffs.CanSire(pawn))
                    return "SimpleTrans.Surgery.AlreadyHasSire".Translate();

                return AcceptanceReport.WasAccepted;
            }

            // Basic check without ingredients - just check recipe name
            var recipeName = recipe?.defName ?? "";
            bool isCarryRecipe = recipeName.Contains("Carry");
            bool isSireRecipe = recipeName.Contains("Sire");
            
            if (!isCarryRecipe && !isSireRecipe) 
                return "SimpleTrans.Surgery.InvalidRecipeType".Translate();

            if (isCarryRecipe && SimpleTransHediffs.CanCarry(pawn))
                return "SimpleTrans.Surgery.AlreadyHasCarry".Translate();
            if (isSireRecipe && SimpleTransHediffs.CanSire(pawn))
                return "SimpleTrans.Surgery.AlreadyHasSire".Translate();

            return AcceptanceReport.WasAccepted;
        }

        private bool HasMatchingHediff(Pawn pawn, string ingredientDefName)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            
            // Check for exact matches
            switch (ingredientDefName)
            {
                case "CarryingOrgans":
                    // Natural carry - check if they have natural (no prosthetic modifiers)
                    return hediffs.Any(h => h.def.defName == "PregnancyCarry") && 
                           !hediffs.Any(h => h.def.defName == "BasicProstheticCarry" || h.def.defName == "BionicProstheticCarry");
                
                case "SiringOrgans":
                    // Natural sire - check if they have natural (no prosthetic modifiers)
                    return hediffs.Any(h => h.def.defName == "PregnancySire") && 
                           !hediffs.Any(h => h.def.defName == "BasicProstheticSire" || h.def.defName == "BionicProstheticSire");
                
                case "BasicReproductiveProsthetic_Carry":
                    return hediffs.Any(h => h.def.defName == "BasicProstheticCarry");
                
                case "BasicReproductiveProsthetic_Sire":
                    return hediffs.Any(h => h.def.defName == "BasicProstheticSire");
                
                case "BionicReproductiveProsthetic_Carry":
                    return hediffs.Any(h => h.def.defName == "BionicProstheticCarry");
                
                case "BionicReproductiveProsthetic_Sire":
                    return hediffs.Any(h => h.def.defName == "BionicProstheticSire");
            }
            
            return false;
        }
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            Log.Message($"[Simple Trans] AddReproductiveAbility.ApplyOnPawn called for {pawn.Name}");
            
            var ingredient = GetReproductiveIngredient(ingredients);
            if (ingredient == null)
            {
                Log.Warning($"[Simple Trans] No reproductive ingredient found!");
                return;
            }

            if (billDoer != null && !CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                var ingredientAbility = GetIngredientAbilityType(ingredients);
                
                Log.Message($"[Simple Trans] Ingredient: {ingredient.Label}, Ability: {ingredientAbility}");

                // Handle vanilla Sterilized hediff conversion before adding new abilities
                ConvertVanillaSterilizedForAdd(pawn, ingredientAbility);
                
                // Add the appropriate hediffs based on ingredient type
                var hediffsToAdd = GetHediffsForIngredient(ingredient);
                if (hediffsToAdd != null && hediffsToAdd.Count > 0)
                {
                    foreach (var hediffName in hediffsToAdd)
                    {
                        var hediffDef = DefDatabase<HediffDef>.GetNamed(hediffName);
                        var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                        pawn.health.AddHediff(hediff);
                        Log.Message($"[Simple Trans] Added hediff: {hediffName}");
                    }
                }
                else
                {
                    Log.Warning($"[Simple Trans] Could not determine hediffs for ingredient: {ingredient.def.defName}");
                    return;
                }

                // Record tale for surgery
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);

                Log.Message($"[Simple Trans] After surgery - CanCarry: {SimpleTransHediffs.CanCarry(pawn)}, CanSire: {SimpleTransHediffs.CanSire(pawn)}");
                
                // Use appropriate message based on ingredient type
                string messageKey = ingredient.def.defName.Contains("Prosthetic") ? "SimpleTransProstheticInstalled" : "SimpleTransOrganTransplanted";
                Messages.Message(messageKey.Translate(pawn.Named("PAWN"), ingredient.Label), 
                    pawn, MessageTypeDefOf.PositiveEvent);
            }
        }

        private Thing GetReproductiveIngredient(List<Thing> ingredients)
        {
            return ingredients.FirstOrDefault(x => 
                x.def.defName.Contains("Organs") || 
                x.def.defName.Contains("ReproductiveProsthetic"));
        }

        private SimpleTransPregnancy.AbilityType? GetIngredientAbilityType(List<Thing> ingredients)
        {
            var ingredient = GetReproductiveIngredient(ingredients);
            if (ingredient == null) return null;

            if (ingredient.def.defName.Contains("Carry")) return SimpleTransPregnancy.AbilityType.Carry;
            if (ingredient.def.defName.Contains("Sire")) return SimpleTransPregnancy.AbilityType.Sire;

            return null;
        }

        private List<string> GetHediffsForIngredient(Thing ingredient)
        {
            var defName = ingredient.def.defName;
            
            // Natural organs - just the base ability
            if (defName == "CarryingOrgans") return new List<string> { "PregnancyCarry" };
            if (defName == "SiringOrgans") return new List<string> { "PregnancySire" };
            
            // Basic prosthetics - base ability + prosthetic modifier
            if (defName == "BasicReproductiveProsthetic_Carry") return new List<string> { "PregnancyCarry", "BasicProstheticCarry" };
            if (defName == "BasicReproductiveProsthetic_Sire") return new List<string> { "PregnancySire", "BasicProstheticSire" };
            
            // Bionic prosthetics - base ability + bionic modifier
            if (defName == "BionicReproductiveProsthetic_Carry") return new List<string> { "PregnancyCarry", "BionicProstheticCarry" };
            if (defName == "BionicReproductiveProsthetic_Sire") return new List<string> { "PregnancySire", "BionicProstheticSire" };
            
            return null;
        }
        
        /// <summary>
        /// Converts vanilla Sterilized hediff to capability-specific sterilization when adding new abilities
        /// Uses opposite capability logic - if adding carry, sterilize siring (and vice versa)
        /// </summary>
        /// <param name="pawn">The pawn being operated on</param>
        /// <param name="newAbility">The ability being added</param>
        private void ConvertVanillaSterilizedForAdd(Pawn pawn, SimpleTransPregnancy.AbilityType? newAbility)
        {
            // Use the core conversion function with opposite capability logic
            SimpleTransCore.ConvertVanillaSterilizedHediffCore(pawn, useOppositeCapabilityLogic: true, targetAbility: newAbility);
        }
    }
}