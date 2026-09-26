using HarmonyLib;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(PawnRenderer), "GetDrawParms")]
    public static class PawnRenderer_GetDrawParms_Patch
    {
        public static void Prefix(PawnRenderer __instance, Vector3 rootLoc, ref float angle, ref PawnRenderFlags flags)
        {
            if (__instance.pawn.IsBathingNow())
            {
                flags &= ~PawnRenderFlags.NoBody;
            }
        }
    }
}
