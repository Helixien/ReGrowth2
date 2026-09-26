using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ReGrowthCore
{
    public class HediffCompProperties_PreventThoughts : HediffCompProperties
    {
        public List<ThoughtDef> thoughtsToPrevent;
        public HediffCompProperties_PreventThoughts()
        {
            this.compClass = typeof(HediffCompPreventThought);
        }
    }
    public class HediffCompPreventThought : HediffComp
    {
        public HediffCompProperties_PreventThoughts Props => base.props as HediffCompProperties_PreventThoughts;
        public bool PreventsThought(ThoughtDef thoughtDef)
        {
            return Props.thoughtsToPrevent.Contains(thoughtDef);
        }
    }
}
