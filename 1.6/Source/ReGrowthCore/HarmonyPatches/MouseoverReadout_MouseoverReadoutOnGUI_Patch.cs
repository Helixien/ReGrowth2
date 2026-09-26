using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(MouseoverReadout), "MouseoverReadoutOnGUI")]
    public static class MouseoverReadout_MouseoverReadoutOnGUI_Patch
    {
        public static (TerrainDef, string) oldTerrainLabel;

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            foreach (var instruction in codeInstructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder lb
                    && lb.LocalIndex == 9)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 9);
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(CellInspectorDrawer_DrawMapInspector_Patch), "TerrainLabelReplacement"));
                }
            }
        }

        public static void TerrainLabelReplacement(TerrainDef def)
        {
            var map = Find.CurrentMap;
            if (def.TryGetBiomeSpecificTerrain(map, out var biomeTerrain))
            {
                if (biomeTerrain.label.NullOrEmpty() is false)
                {
                    oldTerrainLabel = (def, def.label);
                    def.label = biomeTerrain.label;
                    def.cachedLabelCap = def.label.CapitalizeFirst();
                }
            }
        }

        public static void Postfix()
        {
            if (oldTerrainLabel.Item1 != null)
            {
                oldTerrainLabel.Item1.label = oldTerrainLabel.Item2;
                oldTerrainLabel.Item1.cachedLabelCap = oldTerrainLabel.Item1.label.CapitalizeFirst();
            }
        }
    }
}

