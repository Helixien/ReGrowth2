using ModSettingsFramework;
using Verse;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Xml;

namespace ReGrowthCore
{
    public class ReGrowthCore_ScatteredFlames : PatchOperationWorker
    {
        private static ReGrowthCore_ScatteredFlames _handle;
        public static ReGrowthCore_ScatteredFlames ModSettings => _handle ??= LoadedModManager.GetMod<ReGrowthMod>().Content
            .Patches.OfType<ReGrowthCore_ScatteredFlames>().FirstOrDefault();

        public bool enableScatteredFlames = true;
        public bool multiFlames = true;
        public bool specialFX = true;
        public bool smoke = true;
        public bool optimizeShadows = true;
        public bool disableFireWatcher = false;
        public bool enableIgniteGizmo = false;

        private List<PatchOperation> operations = new List<PatchOperation>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableScatteredFlames, "enableScatteredFlames", true);
            Scribe_Values.Look(ref multiFlames, "multiFlames", true);
            Scribe_Values.Look(ref specialFX, "specialFX", true);
            Scribe_Values.Look(ref smoke, "smoke", true);
            Scribe_Values.Look(ref optimizeShadows, "optimizeShadows", true);
            Scribe_Values.Look(ref disableFireWatcher, "disableFireWatcher");
            Scribe_Values.Look(ref enableIgniteGizmo, "enableIgniteGizmo");
        }

        public override void CopyFrom(PatchOperationWorker other)
        {
            if (other is ReGrowthCore_ScatteredFlames s)
            {
                enableScatteredFlames = s.enableScatteredFlames;
                multiFlames = s.multiFlames;
                specialFX = s.specialFX;
                smoke = s.smoke;
                optimizeShadows = s.optimizeShadows;
                disableFireWatcher = s.disableFireWatcher;
                enableIgniteGizmo = s.enableIgniteGizmo;
            }
        }

        public override void DoSettings(ModSettingsContainer container, Listing_Standard list)
        {
            DoCheckbox(list, "RG.EnableScatteredFlames".Translate(), ref enableScatteredFlames, "RG.EnableScatteredFlames.Desc".Translate());
            if (enableScatteredFlames)
            {
                DoCheckbox(list, "ScatteredFlames.Settings.MultiFlames".Translate(), ref multiFlames, "ScatteredFlames.Settings.MultiFlames.Desc".Translate());
                DoCheckbox(list, "ScatteredFlames.Settings.SpecialFX".Translate(), ref specialFX, "ScatteredFlames.Settings.SpecialFX.Desc".Translate());
                if (ScatteredFlamesUtility.smokeInstalled) DoCheckbox(list, "ScatteredFlames.Settings.Smoke".Translate(), ref smoke, "ScatteredFlames.Settings.Smoke.Desc".Translate());
                else
                {
                    smoke = false;
                    DoCheckbox(list, "ScatteredFlames.Settings.Smoke".Translate().Colorize(Color.gray), ref smoke, "ScatteredFlames.Settings.Smoke.Desc".Translate());
                }
                DoCheckbox(list, "ScatteredFlames.Settings.OptimizeShadows".Translate(), ref optimizeShadows, "ScatteredFlames.Settings.OptimizeShadows.Desc".Translate());
                DoCheckbox(list, "ScatteredFlames.Settings.FireWatcher".Translate(), ref disableFireWatcher, "ScatteredFlames.Settings.FireWatcher.Desc".Translate());
                DoCheckbox(list, "ScatteredFlames.Settings.IgniteGizmo".Translate(), ref enableIgniteGizmo, "ScatteredFlames.Settings.IgniteGizmo.Desc".Translate());
            }
        }

        public override void Reset()
        {
            enableScatteredFlames = true;
            multiFlames = true;
            specialFX = true;
            smoke = true;
            optimizeShadows = true;
            disableFireWatcher = false;
            enableIgniteGizmo = false;
        }

        public override void ApplySettings()
        {
            base.ApplySettings();
            ScatteredFlamesUtility.Setup();
        }

        public override bool ApplyWorker(XmlDocument xml)
        {
            if (enableScatteredFlames)
            {
                foreach (PatchOperation operation in operations)
                {
                    if (!operation.Apply(xml))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
