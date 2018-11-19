using System;

namespace Kabala.Lyra2
{
    public static class LyraConstants
	{
		public const UInt64 UINT64_SIZE = 8;
		public const UInt64 BLOCK_LEN_INT64 = 12; //Block length: 768 bits (=96 bytes, =12 uint64_t)
		public const UInt64 BLOCK_LEN_BYTES = BLOCK_LEN_INT64 * UINT64_SIZE;

		public static ulong CalculateInt64LowLength(ulong nCols)
		{
			return BLOCK_LEN_INT64 * nCols;
		}

		public static ulong CalculateBytesLowLength(ulong nCols)
		{
			return CalculateInt64LowLength(nCols) * UINT64_SIZE;
		}
	}
}
