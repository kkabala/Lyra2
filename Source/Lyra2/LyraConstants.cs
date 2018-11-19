/***********************************************************************

	Lyra2 .NET implementation written by Krzysztof Kabała <krzykab@gmail.com>.
	It is distributed under MIT license.
	It is based on C implementation written by The Lyra PHC Team, 
	which can be found here: https://github.com/gincoin-dev/gincoin-core/blob/master/src/crypto/Lyra2Z/Lyra2.c 
	Distribution of this file or code is only allowed with this header.


/***********************************************************************/
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
