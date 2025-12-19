using ModSettingsFramework;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    public class ReGrowthCore_PerspectiveOres : PatchOperationWorker
    {
        public HashSet<string> skippedMineableDefs = [nameof(ThingDefOf.MineableComponentsIndustrial)]; // Need to use string here, as ThingDef aren't initialized yet
        public static Vector2 scrollPos;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref skippedMineableDefs, "skippedMineableDefs", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                skippedMineableDefs ??= [nameof(ThingDefOf.MineableComponentsIndustrial)]; // Need to use string here, as ThingDef aren't initialized yet
                // Just in case
                skippedMineableDefs.RemoveWhere(x => x.NullOrEmpty());
            }
        }

        public override void CopyFrom(PatchOperationWorker savedWorker)
        {
            var copy = savedWorker as ReGrowthCore_PerspectiveOres;
            skippedMineableDefs = copy.skippedMineableDefs;
        }

        public override void ApplySettings()
        {
            base.ApplySettings();
            if (Current.ProgramState == ProgramState.Playing)
            {
                foreach (var map in Find.Maps)
                {
                    Map_FinalizeInit_Patch.ProcessMap(map, true);
                }
            }
        }

        public override void DoSettings(ModSettingsContainer container, Listing_Standard list)
        {
            var curY = list.curY;
            list.Label("PerspectiveOres.Settings.Header".Translate());
            scrollHeight += list.curY - curY;
            DrawList(list);
        }

        public override void Reset()
        {
            ResetMineables();
        }

        public void ResetMineables()
        {
            skippedMineableDefs = [ThingDefOf.MineableComponentsIndustrial?.defName]; ;
            skippedMineableDefs.RemoveWhere(x => x == null);
        }

        public const int lineHeight = 22; //Text.LineHeight + options.verticalSpacing;

        void DrawList(Listing_Standard options)
        {
            //List out all the unremoved defs from the compiled database
            for (int i = PerspectiveOresUtility.mineableDefs.Count; i-- > 0;)
            {
                ThingDef def = PerspectiveOresUtility.mineableDefs[i];
                if (def != null)
                {
                    DrawListItem(options, def);
                }
            }
        }

        void DrawListItem(Listing_Standard options, ThingDef def)
        {
            //Determine checkbox status...
            bool checkOn = skippedMineableDefs.Contains(def.defName) is false;
            var iconRect = new Rect(options.curX, options.curY, 24, 24);
            Widgets.ThingIcon(iconRect, def);
            options.curX += 24;
            options.columnWidthInt -= 24;
            DoCheckbox(options, def.LabelCap, ref checkOn, null);
            options.curX -= 24;
            options.columnWidthInt += 24;
            //Add to working list if missing
            if (checkOn is false && !skippedMineableDefs.Contains(def.defName)) skippedMineableDefs.Add(def.defName);
            //Remove from working list
            else if (checkOn && skippedMineableDefs.Contains(def.defName)) skippedMineableDefs.Remove(def.defName);
        }
    }
}
