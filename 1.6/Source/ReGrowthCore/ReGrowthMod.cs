using HarmonyLib;
using ModSettingsFramework;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    public class ReGrowthMod : Mod
    {
        public static ModContentPack modPack;
        public ReGrowthMod(ModContentPack pack) : base(pack)
        {
            modPack = pack;

            var harmony = new Harmony("ReGrowthCore.WMB");
            var method = AccessTools.Method(typeof(LoadedModManager), "ApplyPatches",
                new[] { typeof(XmlDocument), typeof(Dictionary<XmlNode, LoadableXmlAsset>) });
            var postfix = AccessTools.Method(typeof(LoadedModManager_ApplyPatches_Patch), "Postfix");
            harmony.Patch(method, postfix: new HarmonyMethod(postfix));

        }

        public static bool worldBeautificationToggle = true;
        public static bool WorldBeautificationToggle
        {
            get
            {
                return worldBeautificationToggle;
            }
            set
            {
                worldBeautificationToggle = value;
                foreach (PlanetLayer planetLayer in Find.WorldGrid.PlanetLayers.Values)
                {
                    planetLayer.WorldDrawLayers.Find(f => f.GetType() == typeof(WorldDrawLayer_Hills))?.SetDirty();
                }

            }
        }

        public static bool WorldBeautificationIsActive
        {
            get
            {
                if (ModSettingsFrameworkSettings.GetModSettingsContainer(ReGrowthMod.modPack.PackageIdPlayerFacing).patchOperationStates.TryGetValue("RG_WorldMapBeautificationProject", out var value) && value)
                {
                    return true;
                }
                return false;
            }
        }

        public static bool DefaultWMBPTextureFallback
        {
            get
            {
                if (ModSettingsFrameworkSettings.GetModSettingsContainer(ReGrowthMod.modPack.PackageIdPlayerFacing).patchOperationStates.TryGetValue("RG_WorldMapBeautificationProject", out var value) && value)
                {
                    return true;
                }
                return false;
            }
        }

    }

    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {

            new Harmony("Helixien.ReGrowthCore").PatchAll();

        }
    }
}
