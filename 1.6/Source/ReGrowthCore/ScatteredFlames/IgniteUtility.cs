using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;
using static ReGrowthCore.ScatteredFlames_ResourceBank;
using Verse.AI;

namespace ReGrowthCore
{
    [HarmonyPatch (typeof(PawnAttackGizmoUtility), nameof(PawnAttackGizmoUtility.GetAttackGizmos))]
    static class ScatteredFlames_Patch_GetGizmos
    {
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn pawn)
        {
            foreach (var value in values) yield return value;
            if (ReGrowthCore_ScatteredFlames.ModSettings.enableScatteredFlames && ReGrowthCore_ScatteredFlames.ModSettings.enableIgniteGizmo && pawn.Drafted && Find.Selector.NumSelected == 1)
            {
                Gizmo igniteGizmo = IgniteUtility.GetIgniteGizmo();

                if ((pawn.WorkTagIsDisabled(WorkTags.Firefighting) || pawn.WorkTagIsDisabled(WorkTags.Firefighting)) && !pawn.story.traits.HasTrait(TraitDefOf.Pyromaniac))
                {
                    igniteGizmo.Disable("ScatteredFlames.Ignite.Scared".Translate());
                }

                yield return igniteGizmo;
            }
        }
    }

	public static class IgniteUtility
	{
        static TargetingParameters targetParam = new TargetingParameters(){
                canTargetBuildings = true,
                canTargetItems = true,
                canTargetLocations = false,
                canTargetSelf = false,
                canTargetPawns = false,
                canTargetFires = false,
                onlyTargetFlammables = true,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = delegate (TargetInfo target)
                {
                    Thing thing = target.Thing;

                    if (thing == null || thing.IsBurning() || thing is Pawn)
                    {
                        return false;
                    }

                    return true;
                }
            };
		public static Gizmo GetIgniteGizmo()
        {
            return new Command_Target
			{
				defaultLabel = "ScatteredFlames.Ignite.Label".Translate(),
				defaultDesc = "ScatteredFlames.Ignite.Desc".Translate(),
				icon = IgniteIcon,
				targetingParams = targetParam,
				hotKey = KeyBindingDefOf.Command_ItemForbid,
				action = delegate(LocalTargetInfo targetInfo)
				{
					Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
					Job job = new Job(JobDefOf.Ignite, targetInfo);
					if ((pawn?.RaceProps?.Humanlike ?? false) && (pawn.jobs?.TryTakeOrderedJob(job, JobTag.DraftedOrder) ?? false))
					{
						pawn.Reserve(targetInfo, job);
					}
				}
			};
        }

		public static TargetingParameters IgniteTargetParams()
        {
            return new TargetingParameters(){
                canTargetBuildings = true,
                canTargetItems = true,
                canTargetLocations = false,
                canTargetSelf = false,
                canTargetPawns = false,
                canTargetFires = false,
                onlyTargetFlammables = true,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = delegate (TargetInfo target)
                {
                    Thing thing = target.Thing;

                    if (thing == null || thing.IsBurning() || thing is Pawn)
                    {
                        return false;
                    }

                    return true;
                }
            };
        }
    }
}