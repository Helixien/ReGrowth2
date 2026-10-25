using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace ReGrowthCore
{
    public class CaravanArrivalAction_VisitAbandonedCamp : CaravanArrivalAction
    {
        private AbandonedCamp abandonedCamp;

        public CaravanArrivalAction_VisitAbandonedCamp()
        {
        }

        public CaravanArrivalAction_VisitAbandonedCamp(AbandonedCamp abandonedCamp)
        {
            this.abandonedCamp = abandonedCamp;
        }

        public override string Label => "RG.VisitCamp".Translate();

        public override string ReportString => "CaravanVisiting".Translate(abandonedCamp.Label);

        public override void Arrived(Caravan caravan)
        {
            abandonedCamp.Visit(caravan);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref abandonedCamp, "abandonedCamp");
        }

        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, AbandonedCamp abandonedCamp)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions(
                () =>
                {
                    if (!SettleInEmptyTileUtility.CanCreateMapAt(abandonedCamp.Tile))
                    {
                        return FloatMenuAcceptanceReport.WithFailMessage("CommandCampFailSiteAlreadyThere".Translate());
                    }
                    if (Find.WorldObjects.AnyMapParentAt(abandonedCamp.Tile))
                    {
                        return FloatMenuAcceptanceReport.WithFailMessage("CommandCampFailExistingWorldObject".Translate());
                    }
                    return true;
                },
                () => new CaravanArrivalAction_VisitAbandonedCamp(abandonedCamp),
                "RG.VisitCamp".Translate(),
                caravan,
                abandonedCamp.Tile,
                abandonedCamp
            );
        }
    }
}
