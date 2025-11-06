using RimWorld;

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
                if (ticksUntilSpawn <= 0 && PlantFallColors_GetFallColorFactor_Patch.fallColorFactor > 0.48f)
                {
                    TryDoSpawn();
                    ResetCountdown();
                }
            }
        }
    }
}
