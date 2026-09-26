using RimWorld;
using Verse;

namespace ReGrowthCore
{
    [HotSwappable]
    public class PuddleFilth : Filth
    {
        public override void TickLong()
        {
            if (base.Map.weatherManager.RainRate > 0.1f)
            {
                disappearAfterTicks += GenTicks.TickLongInterval;
            }
        }
    }
}
