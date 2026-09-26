using HarmonyLib;
using Verse;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using static ReGrowthCore.PerspectiveOresUtility;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
    public static class Map_FinalizeInit_Patch
    {
        public static void Postfix(Map __instance)
        {
            ProcessMap(__instance);
        }

        public static void ProcessMap(Map map, bool reset = false)
        {
            Dictionary<(GraphicData, Color), Graphic> graphicCache = new Dictionary<(GraphicData, Color), Graphic>();
            Dictionary<Building, int> lumps = new Dictionary<Building, int>(); //thingID, lumpID
            Dictionary<int, Color> lumpColors = new Dictionary<int, Color>(); //lumpID, associated stone
            int nextLumpID = 0;

            var list = map.listerThings.listsByGroup[(int)ThingRequestGroup.Everything];
            var settings = ModSettings_PerspectiveOres;
            if (settings.skippedMineableDefs is null)
            {
                settings.ResetMineables();
            }
            for (int i = list.Count; i-- > 0;)
            {
                var thing = list[i];
                if (thing is not Mineable mineable || ModSettings_PerspectiveOres.skippedMineableDefs.Contains(thing.def.defName))
                {
                    if (reset) thing.graphicInt = null; //Reset
                    continue;
                }
                if (!mineable.def.building.isResourceRock || !mineable.def.graphicData.Linked) continue;

                if (!lumps.ContainsKey(mineable))
                {
                    lumps.Add(mineable, nextLumpID);
                    DetermineLump(thing.def, nextLumpID++, thing.Position);
                }
            }

            if (lumps.Count == 0)
            {
                if (reset) map.mapDrawer.RegenerateEverythingNow();
                return; //Found nothing
            }
            if (Prefs.DevMode && DebugSettings.godMode) Log.Message("[Perspective: Ores] identified " + lumps.Count + " resource lumps.");

            AssociateLumps();
            LongEventHandler.QueueLongEvent(() => RecolorMineables(), null, false, null);
            LongEventHandler.QueueLongEvent(() => map.mapDrawer.RegenerateEverythingNow(), null, false, null); // Only regenerate once

            void DetermineLump(ThingDef def, int lumpID, IntVec3 startPos)
            {
                Queue<IntVec3> cellsToProcess = new Queue<IntVec3>();
                cellsToProcess.Enqueue(startPos);
                HashSet<IntVec3> processedCells = new HashSet<IntVec3>(); // Track processed cells to avoid redundant checks

                while (cellsToProcess.Count > 0)
                {
                    IntVec3 pos = cellsToProcess.Dequeue();
                    if (!processedCells.Add(pos)) continue; // Skip if already processed

                    foreach (IntVec3 item in GenAdjFast.AdjacentCells8Way(pos).ToArray())
                    {
                        if (!item.InBounds(map)) continue;

                        var edifice = item.GetEdifice(map);
                        if (edifice == null || edifice.def != def || lumps.ContainsKey(edifice)
                            || edifice is not Mineable mineableEdifice
                            || ModSettings_PerspectiveOres.skippedMineableDefs.Contains(mineableEdifice.def.defName))
                        {
                            continue;
                        }
                        if (!mineableEdifice.def.building.isResourceRock) continue;
                        if (!lumps.ContainsKey(edifice))
                        {
                            lumps.Add(edifice, lumpID);
                            cellsToProcess.Enqueue(item);
                        }
                    }
                }
            }

            void AssociateLumps()
            {
                foreach (var (mineable, lumpID) in lumps) //Key = the mineable thing, Value = its lumpID
                {
                    if (lumpColors.ContainsKey(lumpID)) continue; //This lump has already found its match, skip.

                    //Look around this mineral and try to find a stone
                    foreach (var pos in GenAdjFast.AdjacentCells8Way(mineable.Position))
                    {
                        if (!pos.InBounds(map)) continue;

                        var edifice = pos.GetEdifice(map);
                        if (edifice == null || edifice.def == mineable.def || edifice is not Mineable mineableEdifice) continue;
                        if (mineableEdifice.def.building.isResourceRock || !mineableEdifice.def.building.isNaturalRock) continue;

                        lumpColors.Add(lumpID, edifice.DrawColor);
                        break; // Found a color, no need to check other neighbors for this lump
                    }
                }
            }

            void RecolorMineables()
            {
                foreach (var (mineable, lumpID) in lumps)
                {
                    if (!lumpColors.TryGetValue(lumpID, out Color color)) continue;

                    var graphic = mineable.Graphic.data;
                    //Check if the graphic already exists
                    if (!graphicCache.TryGetValue((graphic, color), out Graphic cachedGraphic))
                    {
                        cachedGraphic = GraphicDatabase.Get(graphic.graphicClass, graphic.texPath,
                            (graphic.shaderType ?? ShaderTypeDefOf.Cutout).Shader, graphic.drawSize, color, graphic.colorTwo, graphic, graphic.shaderParameters, graphic.maskPath);

                        cachedGraphic = GraphicUtility.WrapLinked(cachedGraphic, graphic.linkType);
                        graphicCache.Add((graphic, color), cachedGraphic);
                    }
                    mineable.graphicInt = cachedGraphic;
                }
            }
        }
    }
}
