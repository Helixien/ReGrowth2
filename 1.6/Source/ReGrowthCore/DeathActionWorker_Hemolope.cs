using RimWorld;
using Verse;
using Verse.AI.Group;

namespace ReGrowthCore
{
    public class DeathActionWorker_Hemolope : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse, Lord prevLord)
        {
            FilthMaker.TryMakeFilth(corpse.PositionHeld, corpse.MapHeld, ThingDefOf.Filth_Blood, count: Rand.RangeInclusive(7, 10), shouldPropagate: true);
        }
    }
}
