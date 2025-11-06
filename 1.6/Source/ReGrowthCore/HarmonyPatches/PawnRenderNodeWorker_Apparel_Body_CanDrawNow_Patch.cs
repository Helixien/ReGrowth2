using HarmonyLib;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker_Apparel_Body), "CanDrawNow")]
    public static class PawnRenderNodeWorker_Apparel_Body_CanDrawNow_Patch
    {
        public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref bool __result)
        {
            if (node.tree.pawn.IsBathingNow() && parms.Portrait is false)
            {
                __result = false;
            }
        }
    }
}
