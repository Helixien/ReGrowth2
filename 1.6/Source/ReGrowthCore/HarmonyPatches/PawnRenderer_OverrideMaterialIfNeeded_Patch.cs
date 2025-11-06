using HarmonyLib;
using System.Linq;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(PawnRenderer), "OverrideMaterialIfNeeded")]
    public static class PawnRenderer_OverrideMaterialIfNeeded_Patch
    {
        public static void Postfix(PawnRenderer __instance, ref Material __result, Material original, PawnRenderFlags flags)
        {
            if (flags.FlagSet(PawnRenderFlags.Portrait) is false && __instance.pawn.IsBathingNow())
            {
                __result = ReGrowthUtils.GetBatheMat(original, 0.5f);
            }
        }
    }
}
