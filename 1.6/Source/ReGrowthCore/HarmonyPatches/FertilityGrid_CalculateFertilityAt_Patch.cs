using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(FertilityGrid), "CalculateFertilityAt")]
    public static class FertilityGrid_CalculateFertilityAt_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            foreach (var instruction in codeInstructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Stloc_1)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(FertilityGrid_CalculateFertilityAt_Patch), "GetFertility"));
                    yield return new CodeInstruction(OpCodes.Stloc_1);
                }
            }
        }

        public static float GetFertility(float fertility, FertilityGrid grid, IntVec3 cell)
        {
            var terrain = grid.map.terrainGrid.TerrainAt(cell);
            if (terrain.TryGetBiomeSpecificTerrain(grid.map, out var biomeTerrain))
            {
                if (biomeTerrain.fertility.HasValue)
                {
                    return biomeTerrain.fertility.Value;
                }
            }
            return fertility;
        }
    }
}

