using Verse;

namespace ReGrowthCore
{
    public class BuildingWithRandomGraphic : Building
    {
        public override void PostMake()
        {
            base.PostMake();
            var randomGraphic = (base.Graphic as Graphic_Random) ??
                ((base.Graphic as Graphic_Linked).subGraphic as Graphic_Random);
            this.overrideGraphicIndex = Rand.RangeInclusive(0, randomGraphic.SubGraphicsCount);
        }

        private Graphic pollutedGraphic;

        public Graphic PollutedGraphic(GraphicData graphicData)
        {
            if (pollutedGraphic == null)
            {
                if (graphicData == null)
                {
                    return BaseContent.BadGraphic;
                }
                pollutedGraphic = graphicData.GraphicColoredFor(this);
            }

            return pollutedGraphic;
        }

        public override Graphic Graphic
        {
            get
            {
                if (Spawned)
                {
                    var extension = def.GetModExtension<ThingExtension>();
                    if (extension.pollutedGraphicData != null && Position.IsPolluted(Map))
                    {
                        return PollutedGraphic(extension.pollutedGraphicData);
                    }
                }
                return base.Graphic;
            }
        }
    }
}

