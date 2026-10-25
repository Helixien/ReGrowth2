using RimWorld;
using Verse;

namespace ReGrowthCore
{
    public class CompAutumnLeavesSpawner : CompLeavesSpawnerBase
    {
        public override bool ShouldSpawn()
        {
            if (!parent.Spawned)
            {
                return false;
            }
            return true;
        }

        public override void CheckShouldSpawn()
        {
            if (parent is Plant tree && !tree.LeaflessNow)
            {
                var latitude = Find.WorldGrid.LongLatOf(parent.Map.Tile).y;
                if (ticksUntilSpawn <= 0 && PlantFallColors.GetFallColorFactor(latitude, GenLocalDate.DayOfYear(parent.Map)) > 0.48f)
                {
                    TryDoSpawn();
                    ResetCountdown();
                }
            }
        }
    }
}
