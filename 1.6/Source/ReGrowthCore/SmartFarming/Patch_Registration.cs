using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

namespace ReGrowthCore
{
    //Zone spawn
    [HarmonyPatch(typeof(Zone), nameof(Zone.PostRegister))]
    static class Patch_PostRegister
    {
        static void Postfix(Zone __instance)
        {
            if (!ReGrowthCore_SmartFarming.ModSettings.enabled)
            {
                return;
            }
            try
            {
                if
                (
                    __instance is IPlantToGrowSettable && //Is a grow zone?
                    ReGrowthCore_SmartFarming.compCache.TryGetValue(__instance.Map?.uniqueID ?? -1, out MapComponent_SmartFarming comp) && //Can find map component?
                    !comp.growZoneRegistry.ContainsKey(__instance.ID) //Zone data not yet made?
                )
                {
                    var zoneData = new ZoneData();
                    comp.growZoneRegistry.Add(__instance.ID, zoneData);
                    zoneData.Init(comp, __instance);
                    if (Prefs.DevMode && ReGrowthCore_SmartFarming.ModSettings.logging) Log.Message("Zone ID " + __instance.ID + " registered.");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error("[Smart Farming] Error registering new grow zone:\n" + ex);
            }
        }
    }

    //Zone delete
    [HarmonyPatch(typeof(Zone), nameof(Zone.Deregister))]
    static class Patch_Deregister
    {
        static void Prefix(Zone __instance)
        {
            if (__instance is IPlantToGrowSettable) ReGrowthCore_SmartFarming.compCache.TryGetValue(__instance.Map?.uniqueID ?? -1)?.growZoneRegistry?.Remove(__instance.ID);
        }
    }

    //Change plant type
    [HarmonyPatch]
    static class Patch_SetPlantDefToGrow
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(Zone).AllSubclasses()
                .Where(t => typeof(IPlantToGrowSettable).IsAssignableFrom(t))
                .Select(t => AccessTools.DeclaredMethod(t, "SetPlantDefToGrow"))
                .Where(m => m != null);
        }
        static void Postfix(Zone __instance)
        {
            if (ReGrowthCore_SmartFarming.compCache.TryGetValue(__instance.zoneManager.map.uniqueID, out MapComponent_SmartFarming mapComp))
            {
                mapComp.CalculateAll(__instance);
            }
        }
    }

    //Zone expand
    [HarmonyPatch]
    static class Patch_AddCell
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(Zone).AllSubclasses()
                .Where(t => typeof(IPlantToGrowSettable).IsAssignableFrom(t))
                .Select(t => AccessTools.DeclaredMethod(t, "AddCell"))
                .Where(m => m != null);
        }
        static void Postfix(Zone __instance)
        {
            if (ReGrowthCore_SmartFarming.compCache.TryGetValue(__instance.zoneManager.map.uniqueID, out MapComponent_SmartFarming mapComp))
            {
                mapComp.CalculateAll(__instance);
                if (mapComp.growZoneRegistry.TryGetValue(__instance.ID, out ZoneData zoneData)) zoneData.CalculateCornerCell(__instance);
            }
        }
    }

    //Zone shrink
    [HarmonyPatch(typeof(Zone), nameof(Zone.RemoveCell))]
    static class Patch_RemoveCell
    {
        static void Postfix(Zone __instance)
        {
            if (__instance is IPlantToGrowSettable && __instance.cells.Count > 0 && ReGrowthCore_SmartFarming.compCache.TryGetValue(__instance.zoneManager.map.uniqueID, out MapComponent_SmartFarming mapComp))
            {
                mapComp.CalculateAll(__instance);
                if (mapComp.growZoneRegistry.TryGetValue(__instance.ID, out ZoneData zoneData)) zoneData.CalculateCornerCell(__instance);
            }
        }
    }

    //Flush the cache on reload
    [HarmonyPatch(typeof(World), nameof(World.FinalizeInit))]
    public class Patch_LoadGame
    {
        static void Prefix()
        {
            ReGrowthCore_SmartFarming.compCache.Clear();
        }
    }
}
