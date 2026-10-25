using RimWorld;
using Verse;

namespace ReGrowthCore
{
    public sealed class CompProperties_StateAfterHealthLoss : CompProperties
    {
        public int healthPercent = 20;
        public int tickInterval = 100;
        public MentalStateDef mentalState;

        public CompProperties_StateAfterHealthLoss()
        {
            compClass = typeof(CompStateAfterHealthLoss);
        }
    }

    public sealed class CompStateAfterHealthLoss : ThingComp
    {
        private CompProperties_StateAfterHealthLoss Props =>
            (CompProperties_StateAfterHealthLoss)props;

        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            var pawn = parent as Pawn;
            if (pawn == null || pawn.Map == null || pawn.Dead || pawn.Downed
                || !pawn.IsHashIntervalTick(Props.tickInterval, delta))
            {
                return;
            }

            if (pawn.health.summaryHealth.SummaryHealthPercent < Props.healthPercent / 100f)
            {
                // Preserve the original forced-state attempt; the handler decides whether it can start.
                pawn.mindState.mentalStateHandler.TryStartMentalState(
                    Props.mentalState ?? MentalStateDefOf.PanicFlee, forced: true);
            }
        }
    }
}
