using RimWorld;
using Verse;

namespace ReGrowthCore
{
    [HotSwappable]
    public class PuddleFilth : Filth
    {
        public override void Tick()
        {
            base.Tick();
            if (base.Spawned && base.Map.weatherManager.RainRate > 0.1f && this.IsHashIntervalTick(60))
            {
                disappearAfterTicks += 60;
            }
        }
    }
}
