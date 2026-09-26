using Verse;

namespace ReGrowthCore
{
    public class DefExtension_ShaderSpeedMult : DefModExtension
    {
        private float timeMult = 1;

        public void DoWork(TerrainDef def) =>
            def.waterDepthMaterial.SetFloat("_GameSeconds", Find.TickManager.TicksGame * this.timeMult);
    }
}
