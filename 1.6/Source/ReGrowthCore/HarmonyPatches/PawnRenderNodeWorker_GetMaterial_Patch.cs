using HarmonyLib;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker), "GetMaterial")]
    public static class PawnRenderNodeWorker_GetMaterial_Patch
    {
        public static void Postfix(ref Material __result, PawnRenderNodeWorker __instance, PawnRenderNode node, PawnDrawParms parms)
        {
            if (NodeIsBodyOrHasParentBody(node))
            {
                if (parms.Portrait is false && parms.pawn.IsBathingNow())
                {
                    var transparency = 0.5f;
                    if (__instance is PawnRenderNodeWorker_Body
                        && __instance is not PawnRenderNodeWorker_AttachmentBody
                        || node is PawnRenderNode_Body)
                    {
                        transparency = 0.1f;
                    }
                    __result = ReGrowthUtils.GetBatheMat(__result, transparency);
                }
            }
        }

        private static bool NodeIsBodyOrHasParentBody(PawnRenderNode node)
        {
            if (node is PawnRenderNode_Body)
            {
                return true;
            }
            if (node.parent is not null)
            {
                return NodeIsBodyOrHasParentBody(node.parent);
            }
            return false;
        }
    }
}
