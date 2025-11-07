using ModSettingsFramework;
using Verse;
using System.Linq;

namespace ReGrowthCore
{
    [HotSwappable]
    public class ReGrowthCore_Puddles : PatchOperationWorker
    {
        private static ReGrowthCore_Puddles _handle;
        public static ReGrowthCore_Puddles ModSettings => _handle ??= LoadedModManager.GetMod<ReGrowthMod>().Content
            .Patches.OfType<ReGrowthCore_Puddles>().FirstOrDefault();

        public bool rainWaterPuddles = true;
        public bool rainCleanWaterPuddles = false;
        public float puddleChance = 0.2f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref rainWaterPuddles, "rainWaterPuddles", true);
            Scribe_Values.Look(ref rainCleanWaterPuddles, "rainCleanWaterPuddles", false);
            Scribe_Values.Look(ref puddleChance, "puddleChance", 0.2f);
        }

        public override void CopyFrom(PatchOperationWorker other)
        {
            if (other is ReGrowthCore_Puddles s)
            {
                rainWaterPuddles = s.rainWaterPuddles;
                rainCleanWaterPuddles = s.rainCleanWaterPuddles;
                puddleChance = s.puddleChance;
            }
        }

        public override void DoSettings(ModSettingsContainer container, Listing_Standard list)
        {
            DoCheckbox(list, "RG.RainWaterPuddles".Translate(), ref rainWaterPuddles, "RG.RainWaterPuddles.Desc".Translate());
            if(rainWaterPuddles)
            {
                DoSlider(list, "RG.PuddleChance".Translate(), ref puddleChance, puddleChance.ToStringPercent(), 0.01f, 1f, "RG.PuddleChance.Desc".Translate());
                DoCheckbox(list, "RG.RainCleanWaterPuddles".Translate(), ref rainCleanWaterPuddles, "RG.RainCleanWaterPuddles.Desc".Translate());
            }
        }

        public override void Reset()
        {
            rainWaterPuddles = true;
            rainCleanWaterPuddles = false;
            puddleChance = 0.2f;
        }
    }
}
