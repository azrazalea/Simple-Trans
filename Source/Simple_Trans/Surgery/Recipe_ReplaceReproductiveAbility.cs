using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Simple_Trans
{
    public class Recipe_ReplaceReproductiveAbility : Recipe_Surgery
    {
        private enum PartType { Natural, BasicProsthetic, BionicProsthetic }
        private enum AbilityType { Carry, Sire }

        public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord part = null)
        {
            if (!(thing is Pawn pawn)) return "SimpleTrans.Surgery.MustBePawn".Translate();

            Log.Message($"[Simple Trans] AvailableReport called for {pawn.Name}");

            bool hasCarry = SimpleTransPregnancyUtility.CanCarry(pawn);
            bool hasSire = SimpleTransPregnancyUtility.CanSire(pawn);

            Log.Message($"[Simple Trans] Pawn abilities: hasCarry={hasCarry}, hasSire={hasSire}");

            // Must have at least one ability to replace
            if (!hasCarry && !hasSire) 
                return "SimpleTrans.Surgery.NoAbilitiesToReplace".Translate();

            // Use recipe name to determine ingredient type
            var recipeName = recipe?.defName ?? "";
            Log.Message($"[Simple Trans] Recipe name: {recipeName}");
            
            var ingredientDefName = GetIngredientTypeFromRecipeName(recipeName);
            if (ingredientDefName == null)
                return "SimpleTrans.Surgery.InvalidRecipeType".Translate();
                
            Log.Message($"[Simple Trans] Mapped to ingredient: {ingredientDefName}");

            // Check if trying to replace with exact same type
            if (HasMatchingHediff(pawn, ingredientDefName))
                return "SimpleTrans.Surgery.AlreadyHas".Translate(GetIngredientLabel(ingredientDefName));

            bool isCarryIngredient = ingredientDefName.Contains("Carry");
            bool isSireIngredient = ingredientDefName.Contains("Sire");

            // Check if replacement is valid
            if (isCarryIngredient && hasCarry) return AcceptanceReport.WasAccepted; // Same type replacement
            if (isSireIngredient && hasSire) return AcceptanceReport.WasAccepted;   // Same type replacement
            if (isCarryIngredient && hasSire) return AcceptanceReport.WasAccepted;  // Opposite type replacement
            if (isSireIngredient && hasCarry) return AcceptanceReport.WasAccepted;  // Opposite type replacement

            return "SimpleTrans.Surgery.CannotReplace".Translate();
        }

        private string GetIngredientTypeFromRecipeName(string recipeName)
        {
            switch (recipeName)
            {
                case "ReplaceWithCarryOrgans":
                    return "CarryingOrgans";
                case "ReplaceWithSireOrgans":
                    return "SiringOrgans";
                case "ReplaceWithBasicCarryProsthetic":
                    return "BasicReproductiveProsthetic_Carry";
                case "ReplaceWithBasicSireProsthetic":
                    return "BasicReproductiveProsthetic_Sire";
                case "ReplaceWithBionicCarryProsthetic":
                    return "BionicReproductiveProsthetic_Carry";
                case "ReplaceWithBionicSireProsthetic":
                    return "BionicReproductiveProsthetic_Sire";
                default:
                    return null;
            }
        }

        private string GetIngredientLabel(string ingredientDefName)
        {
            switch (ingredientDefName)
            {
                case "CarryingOrgans":
                    return "SimpleTrans.Items.NaturalCarryOrgans".Translate();
                case "SiringOrgans":
                    return "SimpleTrans.Items.NaturalSireOrgans".Translate();
                case "BasicReproductiveProsthetic_Carry":
                    return "SimpleTrans.Items.BasicCarryProsthetic".Translate();
                case "BasicReproductiveProsthetic_Sire":
                    return "SimpleTrans.Items.BasicSireProsthetic".Translate();
                case "BionicReproductiveProsthetic_Carry":
                    return "SimpleTrans.Items.BionicCarryProsthetic".Translate();
                case "BionicReproductiveProsthetic_Sire":
                    return "SimpleTrans.Items.BionicSireProsthetic".Translate();
                default:
                    return "unknown";
            }
        }

        private bool HasMatchingHediff(Pawn pawn, string ingredientDefName)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            
            Log.Message($"[Simple Trans] HasMatchingHediff checking {ingredientDefName} for {pawn.Name}");
            foreach (var hediff in hediffs)
            {
                if (hediff.def.defName.Contains("Pregnancy") || hediff.def.defName.Contains("Prosthetic"))
                {
                    Log.Message($"[Simple Trans] Found hediff: {hediff.def.defName}");
                }
            }
            
            // Check for exact matches
            switch (ingredientDefName)
            {
                case "CarryingOrgans":
                    // Natural carry - check if they have natural (no prosthetic modifiers)
                    bool hasCarry = hediffs.Any(h => h.def.defName == "PregnancyCarry");
                    bool hasProsthetic = hediffs.Any(h => h.def.defName == "BasicProstheticCarry" || h.def.defName == "BionicProstheticCarry");
                    bool result = hasCarry && !hasProsthetic;
                    Log.Message($"[Simple Trans] CarryingOrgans check: hasCarry={hasCarry}, hasProsthetic={hasProsthetic}, result={result}");
                    return result;
                
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
            Log.Message($"[Simple Trans] ReplaceReproductiveAbility.ApplyOnPawn called for {pawn.Name}");
            
            var ingredient = GetReproductiveIngredient(ingredients);
            if (ingredient == null)
            {
                Log.Warning($"[Simple Trans] No reproductive ingredient found!");
                return;
            }

            var ingredientType = GetIngredientType(ingredients);
            var ingredientAbility = GetIngredientAbilityType(ingredients);
            
            Log.Message($"[Simple Trans] Ingredient: {ingredient.Label}, Type: {ingredientType}, Ability: {ingredientAbility}");

            bool hasCarry = SimpleTransPregnancyUtility.CanCarry(pawn);
            bool hasSire = SimpleTransPregnancyUtility.CanSire(pawn);

            // Determine what to replace
            AbilityType targetToReplace;
            
            // First priority: same type replacement
            if (ingredientAbility == AbilityType.Carry && hasCarry)
            {
                targetToReplace = AbilityType.Carry;
            }
            else if (ingredientAbility == AbilityType.Sire && hasSire)
            {
                targetToReplace = AbilityType.Sire;
            }
            // Second priority: opposite type replacement
            else if (ingredientAbility == AbilityType.Carry && hasSire)
            {
                targetToReplace = AbilityType.Sire;
            }
            else if (ingredientAbility == AbilityType.Sire && hasCarry)
            {
                targetToReplace = AbilityType.Carry;
            }
            else
            {
                Log.Warning($"[Simple Trans] Could not determine what to replace!");
                return;
            }

            // Extract current part
            ExtractCurrentPart(pawn, targetToReplace);

            // Add new ability with appropriate hediffs based on ingredient type
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

            Log.Message($"[Simple Trans] After surgery - CanCarry: {SimpleTransPregnancyUtility.CanCarry(pawn)}, CanSire: {SimpleTransPregnancyUtility.CanSire(pawn)}");
            
            Messages.Message("SimpleTransOrganTransplanted".Translate(pawn.Named("PAWN"), ingredient.Label), 
                pawn, MessageTypeDefOf.PositiveEvent);
        }

        private Thing GetReproductiveIngredient(List<Thing> ingredients)
        {
            return ingredients.FirstOrDefault(x => 
                x.def.defName.Contains("Organs") || 
                x.def.defName.Contains("ReproductiveProsthetic"));
        }

        private PartType? GetIngredientType(List<Thing> ingredients)
        {
            var ingredient = GetReproductiveIngredient(ingredients);
            if (ingredient == null) return null;

            if (ingredient.def.defName.Contains("Organs")) return PartType.Natural;
            if (ingredient.def.defName.Contains("Basic")) return PartType.BasicProsthetic;
            if (ingredient.def.defName.Contains("Advanced") || ingredient.def.defName.Contains("Bionic")) return PartType.BionicProsthetic;
            
            return null;
        }

        private AbilityType? GetIngredientAbilityType(List<Thing> ingredients)
        {
            var ingredient = GetReproductiveIngredient(ingredients);
            if (ingredient == null) return null;

            if (ingredient.def.defName.Contains("Carry")) return AbilityType.Carry;
            if (ingredient.def.defName.Contains("Sire")) return AbilityType.Sire;
            
            return null;
        }

        private PartType? GetCurrentPartType(Pawn pawn, AbilityType ability)
        {
            // For now, assume all current abilities are natural organs
            // TODO: Add detection for prosthetics when we implement prosthetic hediffs
            if (ability == AbilityType.Carry && SimpleTransPregnancyUtility.CanCarry(pawn))
                return PartType.Natural;
            if (ability == AbilityType.Sire && SimpleTransPregnancyUtility.CanSire(pawn))
                return PartType.Natural;
            
            return null;
        }

        private void ExtractCurrentPart(Pawn pawn, AbilityType targetToReplace)
        {
            if (targetToReplace == AbilityType.Carry && SimpleTransPregnancyUtility.CanCarry(pawn))
            {
                // Determine what type to extract based on current hediffs
                var itemToSpawn = GetItemForCurrentCarryType(pawn);
                Thing extractedOrgans = ThingMaker.MakeThing(ThingDef.Named(itemToSpawn));
                GenPlace.TryPlaceThing(extractedOrgans, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                
                // Remove all carry-related hediffs
                RemoveCarryHediffs(pawn);
                
                Messages.Message("SimpleTransOrganExtracted".Translate(pawn.Named("PAWN"), extractedOrgans.Label), 
                    pawn, MessageTypeDefOf.NeutralEvent);
            }
            else if (targetToReplace == AbilityType.Sire && SimpleTransPregnancyUtility.CanSire(pawn))
            {
                // Determine what type to extract based on current hediffs
                var itemToSpawn = GetItemForCurrentSireType(pawn);
                Thing extractedOrgans = ThingMaker.MakeThing(ThingDef.Named(itemToSpawn));
                GenPlace.TryPlaceThing(extractedOrgans, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                
                // Remove all sire-related hediffs
                RemoveSireHediffs(pawn);
                
                Messages.Message("SimpleTransOrganExtracted".Translate(pawn.Named("PAWN"), extractedOrgans.Label), 
                    pawn, MessageTypeDefOf.NeutralEvent);
            }
        }

        private string GetItemForCurrentCarryType(Pawn pawn)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            
            foreach (var hediff in hediffs)
            {
                if (hediff.def.defName == "BionicProstheticCarry")
                    return "BionicReproductiveProsthetic_Carry";
                if (hediff.def.defName == "BasicProstheticCarry")
                    return "BasicReproductiveProsthetic_Carry";
            }
            
            return "CarryingOrgans";
        }

        private string GetItemForCurrentSireType(Pawn pawn)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            
            foreach (var hediff in hediffs)
            {
                if (hediff.def.defName == "BionicProstheticSire")
                    return "BionicReproductiveProsthetic_Sire";
                if (hediff.def.defName == "BasicProstheticSire")
                    return "BasicReproductiveProsthetic_Sire";
            }
            
            return "SiringOrgans";
        }

        private void RemoveCarryHediffs(Pawn pawn)
        {
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
        }

        private void RemoveSireHediffs(Pawn pawn)
        {
            var hediffsToRemove = new List<Hediff>();
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.def.defName == "PregnancySire" ||
                    hediff.def.defName == "BasicProstheticSire" ||
                    hediff.def.defName == "BionicProstheticSire")
                {
                    hediffsToRemove.Add(hediff);
                }
            }
            
            foreach (var hediff in hediffsToRemove)
            {
                pawn.health.RemoveHediff(hediff);
            }
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
    }
}