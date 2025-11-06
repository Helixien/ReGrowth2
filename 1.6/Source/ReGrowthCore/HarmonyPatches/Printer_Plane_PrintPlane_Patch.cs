using HarmonyLib;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    [HotSwappable]
    [HarmonyPatch(typeof(Printer_Plane), nameof(Printer_Plane.PrintPlane))]
    public static class Printer_Plane_PrintPlane_Patch
    {
        public static void Prefix(ref Vector3 center)
        {
            if (Thing_Print_Patch.snowAltitudeTweak)
            {
                center += Altitudes.AltIncVect * 5;
            }
        }
    }
}
