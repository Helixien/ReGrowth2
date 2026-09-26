using ModSettingsFramework;
using Verse;

namespace ReGrowthCore
{
    public class ReGrowthCore_SnowOnWalls : PatchOperationWorker
    {
        public bool snowOnWalls = true;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref snowOnWalls, "snowOnWalls", true);
        }

        public override void DoSettings(ModSettingsContainer container, Listing_Standard list)
        {
            DoCheckbox(list, label, ref snowOnWalls, tooltip);
        }

        public override void Reset()
        {
            snowOnWalls = true;
        }

        public override void CopyFrom(PatchOperationWorker savedWorker)
        {
            if (savedWorker is ReGrowthCore_SnowOnWalls copy)
            {
                snowOnWalls = copy.snowOnWalls;
            }
        }
    }
}
