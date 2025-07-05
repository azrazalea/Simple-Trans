using RimWorld;
using Verse;
using System;

namespace Simple_Trans
{
    public class SimpleTransBiosculpterBase : ThingComp
    {
        public virtual void CycleCompleted(Pawn pawn)
        {
            try
            {
                ApplyCycleEffects(pawn);
            }
            catch (Exception ex)
            {
                Log.Error($"[Simple Trans] Error applying biosculpter cycle effects: {ex}");
            }
        }
        
        protected virtual void ApplyCycleEffects(Pawn pawn)
        {
            // Override in subclasses
        }
    }
}