using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ReGrowthCore
{
    public sealed class NocturnalAnimalExtension : DefModExtension
    {
    }

    [HarmonyPatch(typeof(JobGiver_GetRest), nameof(JobGiver_GetRest.GetPriority))]
    internal static class JobGiver_GetRest_NocturnalAnimals_Patch
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var assignmentLocals = new HashSet<int>();
            var body = __originalMethod.GetMethodBody();
            if (body != null)
            {
                foreach (var local in body.LocalVariables)
                {
                    if (local.LocalType == typeof(TimeAssignmentDef))
                    {
                        assignmentLocals.Add(local.LocalIndex);
                    }
                }
            }

            var replacement = AccessTools.Method(typeof(JobGiver_GetRest_NocturnalAnimals_Patch),
                nameof(OverrideTimeAssignment));
            bool patched = false;
            foreach (var instruction in instructions)
            {
                if (assignmentLocals.Contains(StoredLocalIndex(instruction)))
                {
                    // Keep the game's rest thresholds and early exits; only change the sleep schedule.
                    var loadPawn = new CodeInstruction(OpCodes.Ldarg_1);
                    loadPawn.labels.AddRange(instruction.labels);
                    loadPawn.blocks.AddRange(instruction.blocks);
                    instruction.labels.Clear();
                    instruction.blocks.Clear();
                    yield return loadPawn;
                    yield return new CodeInstruction(OpCodes.Call, replacement);
                    patched = true;
                }
                yield return instruction;
            }

            if (!patched)
            {
                Log.Error("[ReGrowth Animals] Could not patch the nocturnal sleep schedule: no time-assignment local was found.");
            }
        }

        private static int StoredLocalIndex(CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Stloc_0) return 0;
            if (instruction.opcode == OpCodes.Stloc_1) return 1;
            if (instruction.opcode == OpCodes.Stloc_2) return 2;
            if (instruction.opcode == OpCodes.Stloc_3) return 3;
            if (instruction.opcode != OpCodes.Stloc && instruction.opcode != OpCodes.Stloc_S) return -1;
            if (instruction.operand is LocalBuilder builder) return builder.LocalIndex;
            return Convert.ToInt32(instruction.operand);
        }

        private static TimeAssignmentDef OverrideTimeAssignment(TimeAssignmentDef original, Pawn pawn)
        {
            if (pawn == null || pawn.def == null || !pawn.def.HasModExtension<NocturnalAnimalExtension>())
            {
                return original;
            }

            int hour = GenLocalDate.HourOfDay(pawn);
            // Original NocturnalAnimals behavior: sleep from 10:00 through 18:59.
            return hour > 9 && hour < 19 ? TimeAssignmentDefOf.Sleep : TimeAssignmentDefOf.Anything;
        }
    }
}
