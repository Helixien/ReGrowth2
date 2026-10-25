using System.Reflection;
using System.Runtime.Versioning;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

[assembly: AssemblyTitle("ReGrowth Animals")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: TargetFramework(".NETFramework,Version=v4.8")]

namespace ReGrowthCore
{
    public sealed class DoesntFleeExtension : DefModExtension
    {
    }

    [StaticConstructorOnStartup]
    internal static class AnimalBehaviorStartup
    {
        static AnimalBehaviorStartup()
        {
            new Harmony("Helixien.ReGrowthCore.AnimalBehaviors")
                .PatchAll(typeof(AnimalBehaviorStartup).Assembly);
        }
    }

    internal static class AnimalBehaviorUtility
    {
        internal static bool DoesntFlee(Pawn pawn)
        {
            return pawn != null && pawn.def != null
                && pawn.def.HasModExtension<DoesntFleeExtension>();
        }
    }

    [HarmonyPatch(typeof(Pawn_MindState), nameof(Pawn_MindState.StartFleeingBecauseOfPawnAction))]
    internal static class Pawn_MindState_StartFleeingBecauseOfPawnAction_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(Pawn_MindState __instance)
        {
            return __instance == null || !AnimalBehaviorUtility.DoesntFlee(__instance.pawn);
        }
    }

    [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.IsFighting))]
    internal static class PawnUtility_IsFighting_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Pawn pawn, ref bool __result)
        {
            // Match the original animal behavior: an active job suppresses combat fleeing.
            if (pawn != null && pawn.CurJob != null && AnimalBehaviorUtility.DoesntFlee(pawn))
            {
                __result = true;
            }
        }
    }
}
