using System;
using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;

namespace ReGrowthCore
{
    [HotSwappable]
    [HarmonyPatch(typeof(GenStep_RockChunks), "GrowLowRockFormationFrom")]
    public static class GenStep_RockChunks_GrowLowRockFormationFrom_Patch
    {
        private static ThingDef capturedRockWallDef;
        private static List<IntVec3> spawnedVanillaChunkPositions = new List<IntVec3>();
        private static readonly MethodInfo TargetGenSpawnSpawnMethod = AccessTools.Method(
            typeof(GenSpawn),
            nameof(GenSpawn.Spawn),
            new Type[] { typeof(ThingDef), typeof(IntVec3), typeof(Map), typeof(WipeMode) }
        );
        private static readonly FieldInfo CapturedRockWallDefFieldInfo = AccessTools.Field(typeof(GenStep_RockChunks_GrowLowRockFormationFrom_Patch), nameof(capturedRockWallDef));
        private static readonly FieldInfo SpawnedVanillaChunkPositionsFieldInfo = AccessTools.Field(typeof(GenStep_RockChunks_GrowLowRockFormationFrom_Patch), nameof(spawnedVanillaChunkPositions));
        private static readonly MethodInfo ListIntVec3AddMethodInfo = AccessTools.Method(typeof(List<IntVec3>), nameof(List<IntVec3>.Add));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;

            for (int i = 0; i < codes.Count; i++)
            {
                var instruction = codes[i];
                yield return instruction;

                if (found is false && instruction.opcode == OpCodes.Ldloc_1)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Stsfld, CapturedRockWallDefFieldInfo);
                    found = true;
                }

                if (instruction.Calls(TargetGenSpawnSpawnMethod))
                {
                    yield return new CodeInstruction(OpCodes.Ldsfld, SpawnedVanillaChunkPositionsFieldInfo);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6);
                    yield return new CodeInstruction(OpCodes.Callvirt, ListIntVec3AddMethodInfo);
                }
            }
        }

        public static void Postfix(IntVec3 root, Map map)
        {
            try
            {
                if (spawnedVanillaChunkPositions.Count < 10)
                {
                    return;
                }

                string stoneName = capturedRockWallDef.defName;
                ThingDef largeBoulderDef = DefDatabase<ThingDef>.GetNamed("RG_BoulderRockLarge" + stoneName, errorOnFail: false);
                ThingDef mediumBoulderDef = DefDatabase<ThingDef>.GetNamed("RG_BoulderRockMedium" + stoneName, errorOnFail: false);

                if (largeBoulderDef == null || mediumBoulderDef == null) return;

                IntVec3 actualClusterCenter = root;
                if (spawnedVanillaChunkPositions.Any())
                {
                    double avgX = 0;
                    double avgZ = 0;
                    foreach (var pos in spawnedVanillaChunkPositions)
                    {
                        avgX += pos.x;
                        avgZ += pos.z;
                    }
                    actualClusterCenter = new IntVec3(
                        (int)Math.Round(avgX / spawnedVanillaChunkPositions.Count) + Rand.RangeInclusive(-2, 2),
                        0,
                        (int)Math.Round(avgZ / spawnedVanillaChunkPositions.Count) + Rand.RangeInclusive(-2, 2)
                    );
                }

                List<CellRect> occupiedByNewBoulders = new List<CellRect>();
                if (!MapGenerator.PlayerStartSpot.IsValid || actualClusterCenter.DistanceTo(MapGenerator.PlayerStartSpot) >= 10)
                {
                    TrySpawnThingAt(actualClusterCenter, largeBoulderDef, map, occupiedByNewBoulders);
                }

                List<IntVec3> mediumBoulderRelativeCenters = new List<IntVec3>
                {
                    new IntVec3(2, 0, 0), new IntVec3(-2, 0, 0), new IntVec3(0, 0, 2), new IntVec3(0, 0, -2),
                    new IntVec3(2, 0, 2), new IntVec3(2, 0, -2), new IntVec3(-2, 0, 2), new IntVec3(-2, 0, -2)
                };
                mediumBoulderRelativeCenters.Shuffle();
                int numMediumToSpawn = Rand.RangeInclusive(1, 3);
                int spawnedMediumCount = 0;
                for (int i = 0; i < mediumBoulderRelativeCenters.Count && spawnedMediumCount < numMediumToSpawn; i++)
                {
                    IntVec3 mediumPos = actualClusterCenter + mediumBoulderRelativeCenters[i];
                    if (!MapGenerator.PlayerStartSpot.IsValid || mediumPos.DistanceTo(MapGenerator.PlayerStartSpot) >= 10)
                    {
                        if (TrySpawnThingAt(mediumPos, mediumBoulderDef, map, occupiedByNewBoulders))
                        {
                            spawnedMediumCount++;
                        }
                    }
                }

                var smallChunkDef = capturedRockWallDef.building.mineableThing;
                if (smallChunkDef != null)
                {
                    int smallChunksToSpawn = Rand.RangeInclusive(10, 20);
                    int smallChunksSpawned = 0;
                    int spawnRadius = 6;
                    List<IntVec3> potentialSmallChunkCells = new List<IntVec3>();

                    for (int dz = -spawnRadius; dz <= spawnRadius; dz++)
                    {
                        for (int dx = -spawnRadius; dx <= spawnRadius; dx++)
                        {
                            if (dx == 0 && dz == 0 && largeBoulderDef != null) continue;

                            IntVec3 cell = actualClusterCenter + new IntVec3(dx, 0, dz);
                            if (cell.InBounds(map))
                            {
                                bool overlapsExistingBoulder = false;
                                foreach (var rect in occupiedByNewBoulders)
                                {
                                    if (rect.Contains(cell))
                                    {
                                        overlapsExistingBoulder = true;
                                        break;
                                    }
                                }
                                if (!overlapsExistingBoulder)
                                {
                                    potentialSmallChunkCells.Add(cell);
                                }
                            }
                        }
                    }
                    potentialSmallChunkCells.Shuffle();

                    for (int k = 0; k < potentialSmallChunkCells.Count && smallChunksSpawned < smallChunksToSpawn; k++)
                    {
                        if (TrySpawnThingAt(potentialSmallChunkCells[k], smallChunkDef, map, occupiedByNewBoulders))
                        {
                            smallChunksSpawned++;
                        }
                    }
                }
            }
            finally
            {
                spawnedVanillaChunkPositions.Clear();
                capturedRockWallDef = null;
            }
        }

        private static bool TrySpawnThingAt(IntVec3 centerPos, ThingDef thingDef, Map map, List<CellRect> occupiedRects)
        {
            CellRect newRect = GenAdj.OccupiedRect(centerPos, thingDef.defaultPlacingRot, thingDef.size);
            ThingDef rockRubbleDef = ThingDefOf.Filth_RubbleRock;

            if (occupiedRects != null)
            {
                foreach (CellRect existingRect in occupiedRects)
                {
                    if (newRect.Overlaps(existingRect)) return false;
                }
            }

            foreach (IntVec3 cell in newRect.Cells)
            {
                if (!cell.InBounds(map)) return false;

                if (cell.GetRoof(map) != null) return false;

                var terrain = cell.GetTerrain(map);
                if (!terrain.natural) return false;

                var affordanceNeeded = thingDef.terrainAffordanceNeeded ?? TerrainAffordanceDefOf.Heavy;

                if (!terrain.affordances.Contains(affordanceNeeded)) return false;
            }

            foreach (IntVec3 cell in newRect.Cells)
            {
                List<Thing> thingsInCell = cell.GetThingList(map);
                for (int i = thingsInCell.Count - 1; i >= 0; i--)
                {
                    thingsInCell[i].DeSpawn(DestroyMode.Vanish);
                }
            }
            Thing newThing = ThingMaker.MakeThing(thingDef);
            GenSpawn.Spawn(newThing, centerPos, map, thingDef.defaultPlacingRot, WipeMode.Vanish);
            occupiedRects.Add(newRect);

            foreach (IntVec3 neigh in newThing.CellsAdjacent8WayAndInside())
            {
                if (!(Rand.Value < 0.5f))
                {
                    continue;
                }
                if (!neigh.InBounds(map))
                {
                    continue;
                }
                bool badThing = false;
                List<Thing> neighList = neigh.GetThingList(map);
                for (int i = 0; i < neighList.Count; i++)
                {
                    Thing t = neighList[i];
                    if (t.def.category != ThingCategory.Plant && t.def.category != ThingCategory.Item && t.def.category != ThingCategory.Pawn && t.def.defName.Contains("RG_BoulderRock") is false)
                    {
                        badThing = true;
                        break;
                    }
                }
                if (!badThing)
                {
                    FilthMaker.TryMakeFilth(neigh, map, rockRubbleDef);
                }
            }
            return true;
        }
    }
}
