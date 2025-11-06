using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker_Apparel_Head), "HeadgearVisible")]
    public static class PawnRenderNodeWorker_Apparel_Head_HeadgearVisible_Patch
    {
        public static void Postfix(PawnDrawParms parms, ref bool __result)
        {
            if (parms.pawn.IsBathingNow() && parms.Portrait is false)
            {
                __result = false;
            }
        }
    }
}
