using HarmonyLib;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ReGrowthCore
{
    [HotSwappable]
    [HarmonyPatch]
    public static class Printer_Plane_PrintPlane_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Graphic_Linked), nameof(Graphic_Linked.Print));
            yield return AccessTools.Method(typeof(Graphic_LinkedAsymmetric), nameof(Graphic_LinkedAsymmetric.Print));
            yield return AccessTools.Method(typeof(Graphic_LinkedCornerFiller), nameof(Graphic_LinkedCornerFiller.Print));
            yield return AccessTools.Method(typeof(Graphic_LinkedCornerOverlay), nameof(Graphic_LinkedCornerOverlay.Print));
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var printPlaneMethod = AccessTools.Method(typeof(Printer_Plane), nameof(Printer_Plane.PrintPlane));
            var interceptorMethod = AccessTools.Method(typeof(Printer_Plane_PrintPlane_Patch), nameof(PrintPlaneInterceptor));

            foreach (var code in instructions)
            {
                if (code.Calls(printPlaneMethod))
                {
                    yield return new CodeInstruction(OpCodes.Call, interceptorMethod);
                }
                else
                {
                    yield return code;
                }
            }
        }

        public static void PrintPlaneInterceptor(MapDrawLayer layer, Vector3 center, Vector2 size, Material mat, float rot, bool flipUv, Vector2[] uvs, Color32[] colors, float topVerticesAltitudeBias, float uvzPayload)
        {
            if (Thing_Print_Patch.printingSnow)
            {
                center += Altitudes.AltIncVect * 5;
            }
            Printer_Plane.PrintPlane(layer, center, size, mat, rot, flipUv, uvs, colors, topVerticesAltitudeBias, uvzPayload);
        }
    }
}