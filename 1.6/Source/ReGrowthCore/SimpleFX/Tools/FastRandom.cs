using System;

namespace ReGrowthCore
{
	/// <summary>
	/// A fast random number generator for .NET
	/// Colin Green, January 2005
	/// 
	/// September 4th 2005
	///	 Added NextBytesUnsafe() - commented out by default.
	///	 Fixed bug in Reinitialise() - y,z and w variables were not being reset.
	/// 
	/// Key points:
	///  1) Based on a simple and fast xor-shift pseudo random number generator (RNG) specified in: 
	///  Marsaglia, George. (2003). Xorshift RNGs.
	///  http://www.jstatsoft.org/v08/i14/xorshift.pdf
	///  
	///  This particular implementation of xorshift has a period of 2^128-1. See the above paper to see
	///  how this can be easily extened if you need a longer period. At the time of writing I could find no 
	///  information on the period of System.Random for comparison.
	/// 
	///  2) Faster than System.Random. Up to 8x faster, depending on which methods are called.
	/// 
	///  3) Direct replacement for System.Random. This class implements all of the methods that System.Random 
	///  does plus some additional methods. The like named methods are functionally equivalent.
	///  
	///  4) Allows fast re-initialisation with a seed, unlike System.Random which accepts a seed at construction
	///  time which then executes a relatively expensive initialisation routine. This provides a vast speed improvement
	///  if you need to reset the pseudo-random number sequence many times, e.g. if you want to re-generate the same
	///  sequence many times. An alternative might be to cache random numbers in an array, but that approach is limited
	///  by memory capacity and the fact that you may also want a large number of different sequences cached. Each sequence
	///  can each be represented by a single seed value (int) when using FastRandom.
	///  
	///  Notes.
	///  A further performance improvement can be obtained by declaring local variables as static, thus avoiding 
	///  re-allocation of variables on each call. However care should be taken if multiple instances of
	///  FastRandom are in use or if being used in a multi-threaded environment.
	/// 
	/// </summary>
	public class FastRandom
	{
		// The +1 ensures NextDouble doesn't generate 1.0
		const double REAL_UNIT_INT = 1.0 / ((double)int.MaxValue + 1.0);
		const uint Y = 842502087, Z = 3579807591, W = 273326509;
		const double REAL_UNIT_UINT = 1.0 / ((double)uint.MaxValue + 1.0);

		uint x, y, z, w;

		#region Constructors

		/// <summary>
		/// Initialises a new instance using time dependent seed.
		/// </summary>
		public FastRandom()
		{
			// Initialise using the system tick count.
			Reinitialise((int)Environment.TickCount);
		}

		#endregion

		#region Public Methods [Reinitialisation]

		/// <summary>
		/// Reinitialises using an int value as a seed.
		/// </summary>
		/// <param name="seed"></param>
		public void Reinitialise(int seed)
		{
			// The only stipulation stated for the xorshift RNG is that at least one of
			// the seeds x,y,z,w is non-zero. We fulfill that requirement by only allowing
			// resetting of the x seed
			x = (uint)seed;
			y = Y;
			z = Z;
			w = W;
		}

		#endregion

		/// <summary>
		/// Generates a random int over the range 0 to upperBound-1, and not including upperBound.
		/// </summary>
		/// <param name="upperBound"></param>
		/// <returns></returns>
		public int Next(int upperBound)
		{
			if (upperBound < 0)
				throw new ArgumentOutOfRangeException("upperBound", upperBound, "upperBound must be >=0");

			uint t = (x ^ (x << 11));
			x = y; y = z; z = w;

			// The explicit int cast before the first multiplication gives better performance.
			// See comments in NextDouble.
			return (int)((REAL_UNIT_INT * (int)(0x7FFFFFFF & (w = (w ^ (w >> 19)) ^ (t ^ (t >> 8))))) * upperBound);
		}
		public int Next(int lowerBound, int upperBound)
		{
			if (lowerBound > upperBound)
				throw new ArgumentOutOfRangeException("upperBound", upperBound, "upperBound must be >=lowerBound");

			uint t = (x ^ (x << 11));
			x = y; y = z; z = w;

			// The explicit int cast before the first multiplication gives better performance.
			// See comments in NextDouble.
			int range = upperBound - lowerBound;
			if (range < 0)
			{   // If range is <0 then an overflow has occured and must resort to using long integer arithmetic instead (slower).
				// We also must use all 32 bits of precision, instead of the normal 31, which again is slower.	
				return lowerBound + (int)((REAL_UNIT_UINT * (double)(w = (w ^ (w >> 19)) ^ (t ^ (t >> 8)))) * (double)((long)upperBound - (long)lowerBound));
			}

			// 31 bits of precision will suffice if range<=int.MaxValue. This allows us to cast to an int and gain
			// a little more performance.
			return lowerBound + (int)((REAL_UNIT_INT * (double)(int)(0x7FFFFFFF & (w = (w ^ (w >> 19)) ^ (t ^ (t >> 8))))) * (double)range);
		}
		// Buffer 32 bits in bitBuffer, return 1 at a time, keep track of how many have been returned
		// with bitBufferIdx.
		uint bitBuffer;
		uint bitMask = 1;
		/// <summary>
		/// Generates a single random bit.
		/// This method's performance is improved by generating 32 bits in one operation and storing them
		/// ready for future calls.
		/// </summary>
		/// <returns></returns>
		public bool NextBool()
		{
			if (bitMask == 1)
			{
				// Generate 32 more bits.
				uint t = (x ^ (x << 11));
				x = y; y = z; z = w;
				bitBuffer = w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));

				// Reset the bitMask that tells us which bit to read next.
				bitMask = 0x80000000;
				return (bitBuffer & bitMask) == 0;
			}

			return (bitBuffer & (bitMask >>= 1)) == 0;
		}
	}
}
