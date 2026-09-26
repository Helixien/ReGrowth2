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

        private const int MinPuddles = 1;
        private const int MaxPuddles = 15;
        private const int DefaultNormalSpeedPuddles = 6;
        private const int DefaultFastSpeedPuddles = 3;
        private const int DefaultUltrafastSpeedPuddles = 1;

        public bool rainWaterPuddles = true;
        public bool rainCleanWaterPuddles = false;
        public int normalSpeedPuddles = DefaultNormalSpeedPuddles;
        public int fastSpeedPuddles = DefaultFastSpeedPuddles;
        public int ultrafastSpeedPuddles = DefaultUltrafastSpeedPuddles;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref rainWaterPuddles, "rainWaterPuddles", true);
            Scribe_Values.Look(ref rainCleanWaterPuddles, "rainCleanWaterPuddles", false);
            Scribe_Values.Look(ref normalSpeedPuddles, "normalSpeedPuddles", DefaultNormalSpeedPuddles);
            Scribe_Values.Look(ref fastSpeedPuddles, "fastSpeedPuddles", DefaultFastSpeedPuddles);
            Scribe_Values.Look(ref ultrafastSpeedPuddles, "ultrafastSpeedPuddles", DefaultUltrafastSpeedPuddles);
        }

        public override void CopyFrom(PatchOperationWorker other)
        {
            if (other is ReGrowthCore_Puddles s)
            {
                rainWaterPuddles = s.rainWaterPuddles;
                rainCleanWaterPuddles = s.rainCleanWaterPuddles;
                normalSpeedPuddles = s.normalSpeedPuddles;
                fastSpeedPuddles = s.fastSpeedPuddles;
                ultrafastSpeedPuddles = s.ultrafastSpeedPuddles;
            }
        }

        public override void DoSettings(ModSettingsContainer container, Listing_Standard list)
        {
            DoCheckbox(list, "RG.RainWaterPuddles".Translate(), ref rainWaterPuddles, "RG.RainWaterPuddles.Desc".Translate());
            if(rainWaterPuddles)
            {
                DoSlider(list, "RG.PuddlesPerTickNormal".Translate(), ref normalSpeedPuddles, normalSpeedPuddles.ToString(), MinPuddles, MaxPuddles, "RG.PuddlesPerTickNormal.Desc".Translate(DefaultNormalSpeedPuddles));
                DoSlider(list, "RG.PuddlesPerTickFast".Translate(), ref fastSpeedPuddles, fastSpeedPuddles.ToString(), MinPuddles, MaxPuddles, "RG.PuddlesPerTickFast.Desc".Translate(DefaultFastSpeedPuddles));
                DoSlider(list, "RG.PuddlesPerTickUltrafast".Translate(), ref ultrafastSpeedPuddles, ultrafastSpeedPuddles.ToString(), MinPuddles, MaxPuddles, "RG.PuddlesPerTickUltrafast.Desc".Translate(DefaultUltrafastSpeedPuddles));
                DoCheckbox(list, "RG.RainCleanWaterPuddles".Translate(), ref rainCleanWaterPuddles, "RG.RainCleanWaterPuddles.Desc".Translate());
            }
        }

        public override void Reset()
        {
            rainWaterPuddles = true;
            rainCleanWaterPuddles = false;
            normalSpeedPuddles = DefaultNormalSpeedPuddles;
            fastSpeedPuddles = DefaultFastSpeedPuddles;
            ultrafastSpeedPuddles = DefaultUltrafastSpeedPuddles;
        }
    }
}
