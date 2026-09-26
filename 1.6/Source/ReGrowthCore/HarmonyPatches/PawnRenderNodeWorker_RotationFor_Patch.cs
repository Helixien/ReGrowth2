using HarmonyLib;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    [HotSwappable]
    [HarmonyPatch(typeof(PawnRenderNodeWorker), "RotationFor")]
    public static class PawnRenderNodeWorker_RotationFor_Patch
    {
        public static float angleOffset = 45f;

        public static void Postfix(PawnRenderNodeWorker __instance, PawnRenderNode node, PawnDrawParms parms,
            ref Quaternion __result)
        {
            var pawn = node.tree.pawn;
            if (!parms.Portrait && node is PawnRenderNode_Body && (pawn.CurJob?.swimming ?? false))
            {
                if (pawn.CurJob?.swimming ?? false)
                {
                    Rot4 bodyFacing = pawn.Rotation;
                    if (bodyFacing == Rot4.West)
                    {
                        __result *= Quaternion.Euler(Vector3.up * -angleOffset);

                    }
                    else if (bodyFacing == Rot4.East)
                    {
                        __result *= Quaternion.Euler(Vector3.up * angleOffset);
                    }
                }
            }
        }
    }
}
