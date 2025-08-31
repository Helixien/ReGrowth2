using HarmonyLib;
using ModSettingsFramework;
using RimWorld.Planet;
using System;
using System.Linq;
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
                Find.World.renderer.SetDirty<WorldDrawLayer_Hills>(Find.WorldGrid.Surface);
                
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
