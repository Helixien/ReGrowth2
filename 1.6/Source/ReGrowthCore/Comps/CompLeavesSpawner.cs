using RimWorld;

namespace ReGrowthCore
{
    public class CompLeavesSpawner : CompLeavesSpawnerBase
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
                if (ticksUntilSpawn <= 0)
                {
                    TryDoSpawn();
                    ResetCountdown();
                }
            }
        }
    }
}
