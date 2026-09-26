using Verse;

namespace ReGrowthCore
{
    [StaticConstructorOnStartup]
    public static class ModCompatibility
    {
        public static bool DubsBadHygieneActive;
        static ModCompatibility()
        {
            DubsBadHygieneActive = ModLister.AnyModActiveNoSuffix(["Dubwise.DubsBadHygiene", "Dubwise.DubsBadHygiene.Lite"]);
        }

        public static void CleanHygiene(Pawn pawn)
        {
            DubsBadHygiene.Need_Hygiene need = pawn?.needs?.TryGetNeed<DubsBadHygiene.Need_Hygiene>();
            if (need != null)
            {
                need.clean(0.001f * 0.4f);
            }
        }
    }
}
