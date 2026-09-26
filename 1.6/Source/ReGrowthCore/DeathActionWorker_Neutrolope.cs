using RimWorld;
using Verse;
using Verse.AI.Group;

namespace ReGrowthCore
{
    public class DeathActionWorker_Neutrolope : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse, Lord prevLord)
        {
            FilthMaker.TryMakeFilth(corpse.PositionHeld, corpse.MapHeld, RG_DefOf.RG_Filth_Neutroamine, count: Rand.RangeInclusive(7, 10), shouldPropagate: true);
        }
    }
}
