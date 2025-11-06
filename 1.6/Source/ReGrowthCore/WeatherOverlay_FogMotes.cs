using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ReGrowthCore
{
    public class WeatherOverlay_FogMotes : SkyOverlay
    {
        public static Dictionary<Map, CachedResult<List<IntVec3>>> unroofedCells = new();

        public override void DrawOverlay(Map map)
        {
        }

        public override void SetOverlayColor(Color color)
        {
        }

        public override void TickOverlay(Map map, float lerpFactor)
        {
            var extension = map.weatherManager.curWeather?.GetModExtension<WeatherExtension_FogMotes>();
            if (extension != null)
            {
                if (Rand.Chance(extension.fogSpawnRate))
                {
                    if (!unroofedCells.TryGetValue(map, out var cache))
                    {
                        unroofedCells[map] = cache = new CachedResult<List<IntVec3>>(map.AllCells.Where(x => x.Roofed(map) is false).ToList(), 2500);
                    }
                    else if (cache.CacheExpired)
                    {
                        cache.Result = map.AllCells.Where(x => x.Roofed(map) is false).ToList();
                    }
                    if (cache.Result.TryRandomElement(out var cell))
                    {
                        FleckMaker.ThrowSmoke(cell.ToVector3Shifted(), map, extension.fogSize);
                    }
                }
            }
        }
    }

    public class WeatherExtension_FogMotes : DefModExtension
    {
        public float fogSize;

        public float fogSpawnRate;
    }
}
