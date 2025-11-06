using Verse;
using UnityEngine;
using System.Collections.Generic;
using Verse.Sound;
using RimWorld;
using HarmonyLib;
using System.Linq;

namespace ReGrowthCore
{
    [StaticConstructorOnStartup]
    public static class PerspectiveOresUtility
    {
        static PerspectiveOresUtility()
        {
            var list = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = list.Count; i-- > 0;)
            {
                var def = list[i];
                if (def.thingClass == mineable && def.building != null && def.building.isResourceRock)
                    mineableDefs.Add(def);
            }
        }
        public static System.Type mineable = typeof(Mineable);
        public static List<ThingDef> mineableDefs = new List<ThingDef>();

        private static ReGrowthCore_PerspectiveOres _handle;
        public static ReGrowthCore_PerspectiveOres ModSettings_PerspectiveOres => _handle ??= LoadedModManager.GetMod<ReGrowthMod>().Content
            .Patches.OfType<ReGrowthCore_PerspectiveOres>().FirstOrDefault();
    }
}
