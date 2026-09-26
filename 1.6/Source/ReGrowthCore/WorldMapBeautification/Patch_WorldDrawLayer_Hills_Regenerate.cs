using HarmonyLib;
using RimWorld.Planet;
using System.Collections;
using System.Reflection;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(WorldDrawLayer_Hills), nameof(WorldDrawLayer_Hills.Regenerate))]
    public static class Patch_WorldDrawLayer_Hills_Regenerate
    {
        public static bool Prefix(WorldDrawLayer_Hills __instance, ref IEnumerable __result)
        {
            // Log.Message("Looking at layer "+__instance.planetLayer.Def);
            // Log.Message("Has the field? "+__instance.planetLayer.WorldDrawLayers.Any(f => f.GetType() == typeof(WorldDrawLayer_Beautification)));
            if (!ReGrowthMod.WorldBeautificationIsActive ||
                !ReGrowthMod.worldBeautificationToggle ||
                !__instance.planetLayer.WorldDrawLayers.Any(f => f.GetType() == typeof(WorldDrawLayer_Beautification)))
            {
                // Use original WorldDrawLayer_Hills if WMBP is disabled
                // Or the planetLayer doesn't have WorldDrawLayer_Beautification  
                return true;
            }

            // Need to clear meshes so they don't still appear
            MethodInfo clearSubMeshes = typeof(WorldDrawLayerBase).GetMethod(
                "ClearSubMeshes",
                BindingFlags.Instance | BindingFlags.NonPublic);

            clearSubMeshes?.Invoke(__instance, [MeshParts.All]);

            __result = CustomRegenerate();
            return false; // skip original completely
        }

        private static IEnumerable CustomRegenerate()
        {
            yield break;
        }
    }
}
