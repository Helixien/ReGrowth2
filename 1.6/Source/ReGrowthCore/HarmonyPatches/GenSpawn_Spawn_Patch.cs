using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(GenSpawn), "Spawn", new Type[] { typeof(Thing), typeof(IntVec3),
        typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool) })]
    public static class GenSpawn_Spawn_Patch
    {
        public static void Prefix(ref Thing newThing, ref IntVec3 loc, Map map)
        {
            if (SpaceRockManager.isSpawningMeteoriteContents && newThing is Mineable mineable)
            {
                mineable.MarkAsSpaceRock();
            }
        }
    }
}
