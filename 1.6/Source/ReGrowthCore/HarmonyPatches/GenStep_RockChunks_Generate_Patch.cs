using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;

namespace ReGrowthCore
{
    //[HotSwappable]
    //[HarmonyPatch(typeof(GenStep_RockChunks), "Generate")]
    public static class GenStep_RockChunks_Generate_Patch
    {
        public static bool debugMode => false;
        private static float GetSpawnRate(float original)
        {
            return debugMode ? 0.3f : original;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var getSpawnRateMethod = typeof(GenStep_RockChunks_Generate_Patch).GetMethod(nameof(GetSpawnRate), BindingFlags.Static | BindingFlags.NonPublic);

            for (int i = 0; i < codes.Count; i++)
            {
                var instruction = codes[i];
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldc_R4 && Math.Abs((float)instruction.operand - 0.006f) < 0.0001f)
                {
                    yield return new CodeInstruction(OpCodes.Call, getSpawnRateMethod);
                }
            }
        }
    }
}
