using System.Runtime.CompilerServices;
using Verse;

namespace ReGrowthCore
{
    public class CachedResult<T>
    {
        public int lastTickCheck;
        private T _result;
        public int tickCheckInterval;

        public CachedResult(T result, int tickCheckInterval)
        {
            this._result = result;
            this.tickCheckInterval = tickCheckInterval;
        }
        public bool CacheExpired
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return lastTickCheck == 0 || Find.TickManager.TicksGame > lastTickCheck + tickCheckInterval;
            }
        }

        public T Result
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _result;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _result = value;
                lastTickCheck = Find.TickManager.TicksGame;
            }
        }
    }
}
