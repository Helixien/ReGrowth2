using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace ReGrowthCore
{
    [HarmonyPatch(typeof(CompPowerPlantWind), "CompTick")]
    public static class CompPowerPlantWind_CompTick_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var codes = codeInstructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                yield return code;
                if (code.opcode == OpCodes.Stloc_0)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CompPowerPlantWind_CompTick_Patch), nameof(TryChangePowerOutput)));
                }
            }
        }

        private const float MaxUsableWindIntensity = 1.5f;
        private const float MinUsableWindIntensity = 1.2f;
        private const float WindChangeStep = 0.025f;
        public static void TryChangePowerOutput(CompPowerPlantWind comp, ref float num)
        {
            float prevValue = Mathf.Abs(comp.cachedPowerOutput) / Mathf.Abs(comp.Props.PowerConsumption);
            if (comp.parent.Map.weatherManager.CurWeatherPerceived?.GetModExtension<WeatherExtension>()?.increasesWindTurbinesOutput ?? false)
            {
                float targetValue = Mathf.Lerp(MinUsableWindIntensity, MaxUsableWindIntensity, num / MaxUsableWindIntensity);
                float minValue = targetValue > prevValue ? Mathf.Min(targetValue, prevValue + WindChangeStep) : Mathf.Max(targetValue, prevValue - WindChangeStep);
                num = Mathf.Min(minValue, targetValue);
            }
            else if (comp.parent.Map.weatherManager.lastWeather?.GetModExtension<WeatherExtension>()?.increasesWindTurbinesOutput ?? false)
            {
                float diff = Mathf.Abs(num - prevValue);
                if (diff > WindChangeStep)
                {
                    float minValue = num > prevValue ? Mathf.Min(num, prevValue + WindChangeStep) : Mathf.Max(num, prevValue - WindChangeStep);
                    num = Mathf.Max(minValue, num);
                }
            }
        }
    }
}
