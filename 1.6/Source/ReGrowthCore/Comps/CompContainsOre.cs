using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Verse;
using RimWorld;

namespace ReGrowthCore
{
    public class CompProperties_ContainsOre : CompProperties
    {
        public CompProperties_ContainsOre()
        {
            compClass = typeof(CompContainsOre);
        }
        public IntRange amount;
        public List<ThingDef> allowedOreDefs;
        public List<ThingDef> blacklistOres;
        public float chanceToContainOre = 1f;
    }
    public class CompContainsOre : ThingComp
    {
        public CompProperties_ContainsOre Props => (CompProperties_ContainsOre)props;

        public ThingDef chosenOreDef;
        public int chosenAmount;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (!respawningAfterLoad && Rand.Chance(Props.chanceToContainOre))
            {
                this.chosenAmount = Props.amount.RandomInRange;
                if (Props.allowedOreDefs != null && Props.allowedOreDefs.Any())
                {
                    this.chosenOreDef = Props.allowedOreDefs.RandomElement();
                }
                else
                {
                    var deepResources = DefDatabase<ThingDef>.AllDefsListForReading
                        .Where(t => t.deepCommonality > 0 && t.CountAsResource);

                    if (Props.blacklistOres != null && Props.blacklistOres.Any())
                    {
                        deepResources = deepResources.Where(t => !Props.blacklistOres.Contains(t));
                    }

                    if (deepResources.Any())
                    {
                        this.chosenOreDef = deepResources.RandomElementByWeightWithFallback(t => t.deepCommonality);
                    }
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref chosenAmount, "chosenAmount", 0);
            Scribe_Defs.Look(ref chosenOreDef, "chosenOreDef");
        }

        public override string CompInspectStringExtra()
        {
            if (chosenOreDef != null)
            {
                return "RG.ChunkContains".Translate(chosenOreDef.label);
            }
            return base.CompInspectStringExtra();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (mode != DestroyMode.Vanish && parent.def.GetModExtension<BoulderProperties>() is BoulderProperties extension)
            {
                int amount = extension.chunksToSpawn.RandomInRange;
                for (int i = 0; i < amount; i++)
                {
                    Thing thing = ThingMaker.MakeThing(extension.chunkDefToSpawn);
                    GenPlace.TryPlaceThing(thing, parent.Position, previousMap, ThingPlaceMode.Near);
                }
            }
        }
    }
}
