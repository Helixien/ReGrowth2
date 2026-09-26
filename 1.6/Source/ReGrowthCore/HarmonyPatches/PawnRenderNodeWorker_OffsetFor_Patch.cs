using HarmonyLib;
using LudeonTK;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    [HotSwappable]
    [HarmonyPatch(typeof(PawnRenderNodeWorker), "OffsetFor")]
    public static class PawnRenderNodeWorker_OffsetFor_Patch
    {
        public static float xOffset = 0.17f;
        public static float zOffset = 0.035f;
        public static void Postfix(PawnRenderNodeWorker __instance, PawnRenderNode node, PawnDrawParms parms,
            ref Vector3 __result)
        {
            var pawn = node.tree.pawn;
            if (node is PawnRenderNode_Body && pawn.RaceProps.Humanlike && !parms.Portrait && (pawn.CurJob?.swimming ?? false))
            {
                __result.z -= 0.5f;
                Rot4 bodyFacing = pawn.Rotation;
                if (bodyFacing == Rot4.West)
                {
                    __result.x += xOffset;
                    __result.z += zOffset;
                }
                else if (bodyFacing == Rot4.East)
                {
                    __result.x += -xOffset;
                    __result.z += zOffset;
                }
            }
        }
    }
}
