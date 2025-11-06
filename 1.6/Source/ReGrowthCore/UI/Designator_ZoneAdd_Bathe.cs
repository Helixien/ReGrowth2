using RimWorld;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    public class Designator_ZoneAdd_Bathe_Expand : Designator_ZoneAdd_Bathe
    {
        public Designator_ZoneAdd_Bathe_Expand()
        {
            defaultLabel = "DesignatorZoneExpand".Translate();
            hotKey = KeyBindingDefOf.Misc6;
        }
    }
    public class Designator_ZoneAdd_Bathe : Designator_ZoneAdd
    {
        public override string NewZoneLabel => "RG.BatheZone".Translate();
        public Designator_ZoneAdd_Bathe()
        {
            zoneTypeToPlace = typeof(Zone_Bathe);
            defaultLabel = "RG.BatheZone".Translate();
            defaultDesc = "RG.DesignatorBatheZoneDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Zones/ZoneCreate_Bathe");
            hotKey = KeyBindingDefOf.Misc2;
            soundSucceeded = SoundDefOf.Designate_ZoneAdd;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!base.CanDesignateCell(c).Accepted)
            {
                return false;
            }
            return c.GetTerrain(Map).IsWater && c.Walkable(Map);
        }

        public override Zone MakeNewZone()
        {
            return new Zone_Bathe(Find.CurrentMap.zoneManager);
        }
    }
}
