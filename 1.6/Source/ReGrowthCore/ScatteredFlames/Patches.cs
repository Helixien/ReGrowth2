using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using RimWorld.Planet;
using static ReGrowthCore.ScatteredFlamesUtility;
using static ReGrowthCore.ScatteredFlames_ResourceBank;

namespace ReGrowthCore
{
	[HarmonyPatch (typeof(Fire), nameof(Fire.SpawnSetup))]
    static class Patch_SpawnSetup
    {
        static void Postfix(Fire __instance)
        {
            if (!ReGrowthCore_ScatteredFlames.ModSettings.enableScatteredFlames) return;

            if (__instance.parent?.def.category == ThingCategory.Pawn) __instance.graphicInt = ScatteredFlames_ResourceBank.FireGraphic;
            else
            {
                fireCache.Add(__instance.thingIDNumber, new ScatteredFlamesUtility.FlameData(__instance));
                burningCache.Add(__instance.Position);
                somethingBurning = true;
            }
        }
    }

	[HarmonyPatch (typeof(Fire), nameof(Fire.DeSpawn))]
    static class Patch_DeSpawn
    {
        static void Prefix(Fire __instance)
        {
            if (!ReGrowthCore_ScatteredFlames.ModSettings.enableScatteredFlames) return;
            fireCache.Remove(__instance.thingIDNumber);
            burningCache.Remove(__instance.Position);
            somethingBurning = burningCache.Count > 0;
        }
    }

	[HarmonyPatch (typeof(World), nameof(World.FinalizeInit))]
	   static class ScatteredFlames_Patch_World_FinalizeInit
    {
        static void Prefix()
        {
            if (!ReGrowthCore_ScatteredFlames.ModSettings.enableScatteredFlames) return;
            fireCache = new System.Collections.Generic.Dictionary<int, ScatteredFlamesUtility.FlameData>();
            burningCache = new System.Collections.Generic.HashSet<IntVec3>();
            somethingBurning = false;
        }
    }

    [HarmonyPatch (typeof(FireWatcher), nameof(FireWatcher.UpdateObservations))]
    static class Patch_FireWatcher_UpdateObservations
    {
        static bool Prefix()
        {
            if (!ReGrowthCore_ScatteredFlames.ModSettings.enableScatteredFlames) return true;
            return !ReGrowthCore_ScatteredFlames.ModSettings.disableFireWatcher;
        }
    }


    [HarmonyPatch (typeof(TickManager), nameof(TickManager.DoSingleTick))]
    static class Patch_TickManager_DoSingleTick
    {
        static void Postfix()
        {
            if (!ReGrowthCore_ScatteredFlames.ModSettings.enableScatteredFlames) return;
            var tickManager = Current.gameInt.tickManager;
            curTimeSpeed = (int)tickManager.curTimeSpeed;
            if (nextFrame = (tickManager.ticksGameInt % 15) * curTimeSpeed >= 14)
            {
                triggeringFrameID = RealTime.frameCount;
            }
        }
    }

    [HarmonyPatch (typeof(Printer_Shadow), nameof(Printer_Shadow.PrintShadow), new System.Type[]
    { 
        typeof(SectionLayer),
        typeof(Vector3),
        typeof(Vector3),
        typeof(Rot4)
    })]
    static class Patch_PrintShadow
    {
        static bool Prefix(Vector3 center)
        {
            if (!ReGrowthCore_ScatteredFlames.ModSettings.enableScatteredFlames) return true;
            return !(ReGrowthCore_ScatteredFlames.ModSettings.optimizeShadows && somethingBurning && burningCache.Contains(center.ToIntVec3()));
        }
    }
}