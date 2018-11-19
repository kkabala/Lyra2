/***********************************************************************

	Lyra2 .NET implementation written by Krzysztof Kabała <krzykab@gmail.com>.
	It is distributed under MIT license.
	It is based on C implementation written by The Lyra PHC Team, 
	which can be found here: https://github.com/gincoin-dev/gincoin-core/blob/master/src/crypto/Lyra2Z/Lyra2.c 
	Distribution of this file or code is only allowed with this header.


/***********************************************************************/
using System;
using System.Linq;

namespace Kabala.Lyra2
{
    public class Sponge
	{
		private ulong[] state;
		private MemoryMatrix memoryMatrix;

		public Sponge(ulong[] state, MemoryMatrix memoryMatrix)
		{
			this.state = state;
			this.memoryMatrix = memoryMatrix;
		}

		/*Blake2b's G function*/
		private void G(int r, int i, ulong a, ulong b, ulong c, ulong d)
		{
			state[a] = state[a] + state[b];
			state[d] = Rotr64(state[d] ^ state[a], 32);
			state[c] = state[c] + state[d];
			state[b] = Rotr64(state[b] ^ state[c], 24);
			state[a] = state[a] + state[b];
			state[d] = Rotr64(state[d] ^ state[a], 16);
			state[c] = state[c] + state[d];
			state[b] = Rotr64(state[b] ^ state[c], 63);
		}

		private void RoundLyra(int r)
		{
			G(r, 0, 0, 4, 8, 12);
			G(r, 1, 1, 5, 9, 13);
			G(r, 2, 2, 6, 10, 14);
			G(r, 3, 3, 7, 11, 15);
			G(r, 4, 0, 5, 10, 15);
			G(r, 5, 1, 6, 11, 12);
			G(r, 6, 2, 7, 8, 13);
			G(r, 7, 3, 4, 9, 14);
		}

		private ulong Rotr64(ulong w, int c)
		{
			return (w >> c) | (w << (64 - c));
		}

		private void Blake2bLyra(int numberOfIterations)
		{
			for (int i = 0; i < numberOfIterations; i++)
			{
				RoundLyra(i);
			}
		}

		public void AbsorbBlockBlake2Safe(byte[] inByteArray)
		{
			var inArray = ConvertByteToUInt64Array(inByteArray);
			state[0] ^= inArray[0];
			state[1] ^= inArray[1];
			state[2] ^= inArray[2];
			state[3] ^= inArray[3];
			state[4] ^= inArray[4];
			state[5] ^= inArray[5];
			state[6] ^= inArray[6];
			state[7] ^= inArray[7];

			Blake2bLyra(12);
		}

		private ulong[] ConvertByteToUInt64Array(byte[] byteArray)
		{
			var size = byteArray.Length / sizeof(ulong);
			var ints = new ulong[size];
			for (var index = 0; index < size; index++)
			{
				ints[index] = BitConverter.ToUInt64(byteArray, index * sizeof(ulong));
			}

			return ints;
		}

		public void ReducedSqueezeRow0(ulong rowOut, ulong nCols) // I screwed up something here
		{
			for (int i = 0; i < (int)nCols; i++)
			{
				ulong currentCol = nCols - 1 - (ulong)i;
				memoryMatrix.SetColumnWithBlockData(rowOut, currentCol, state.Take(12).ToArray());
				Blake2bLyra(1);
			}
		}

		public void ReducedDuplexRow1(ulong rowIn, ulong rowOut, ulong nCols)
		{
			for (ulong i = 0; i < nCols; i++)
			{
				//Output: goes to previous column
				ulong ptrWordOut = nCols - 1 - i;
				//Input: next column (i.e., next block in sequence)
				ulong ptrWordIn = i;

				//caching the values - for speed purposes and not duplicating the same code
				var ptrWordIn0 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordIn, 0);
				var ptrWordIn1 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordIn, 1);
				var ptrWordIn2 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordIn, 2);
				var ptrWordIn3 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordIn, 3);
				var ptrWordIn4 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordIn, 4);
				var ptrWordIn5 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordIn, 5);
				var ptrWordIn6 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordIn, 6);
				var ptrWordIn7 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordIn, 7);
				var ptrWordIn8 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordIn, 8);
				var ptrWordIn9 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordIn, 9);
				var ptrWordIn10 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordIn, 10);
				var ptrWordIn11 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordIn, 11);

				state[0] ^= ptrWordIn0;
				state[1] ^= ptrWordIn1;
				state[2] ^= ptrWordIn2;
				state[3] ^= ptrWordIn3;
				state[4] ^= ptrWordIn4;
				state[5] ^= ptrWordIn5;
				state[6] ^= ptrWordIn6;
				state[7] ^= ptrWordIn7;
				state[8] ^= ptrWordIn8;
				state[9] ^= ptrWordIn9;
				state[10] ^= ptrWordIn10;
				state[11] ^= ptrWordIn11;

				Blake2bLyra(1);

				var ptrWordOutNewBlockData = new[]
				{
					ptrWordIn0 ^ state[0],
					ptrWordIn1 ^ state[1],
					ptrWordIn2 ^ state[2],
					ptrWordIn3 ^ state[3],
					ptrWordIn4 ^ state[4],
					ptrWordIn5 ^ state[5],
					ptrWordIn6 ^ state[6],
					ptrWordIn7 ^ state[7],
					ptrWordIn8 ^ state[8],
					ptrWordIn9 ^ state[9],
					ptrWordIn10 ^ state[10],
					ptrWordIn11 ^ state[11]
				};

				memoryMatrix.SetColumnWithBlockData(rowOut, ptrWordOut, ptrWordOutNewBlockData);
			}
		}


		public void ReducedDuplexRowSetup(ulong rowIn, ulong rowInOut, ulong rowOut,
			ulong nCols)
		{
			for (ulong i = 0; i < nCols; i++)
			{
				//Output: goes to previous column
				ulong ptrWordOutColumn = nCols - 1 - i;
				//Input: next column (i.e., next block in sequence)
				ulong ptrWordInColumn = i;

				//caching the values - for speed purposes and not duplicating the same code
				var ptrWordIn0 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordInColumn, 0);
				var ptrWordIn1 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordInColumn, 1);
				var ptrWordIn2 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordInColumn, 2);
				var ptrWordIn3 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordInColumn, 3);
				var ptrWordIn4 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordInColumn, 4);
				var ptrWordIn5 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordInColumn, 5);
				var ptrWordIn6 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordInColumn, 6);
				var ptrWordIn7 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordInColumn, 7);
				var ptrWordIn8 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordInColumn, 8);
				var ptrWordIn9 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordInColumn, 9);
				var ptrWordIn10 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordInColumn, 10);
				var ptrWordIn11 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordInColumn, 11);

				var ptrWordInOut0 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordInColumn, 0);
				var ptrWordInOut1 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordInColumn, 1);
				var ptrWordInOut2 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordInColumn, 2);
				var ptrWordInOut3 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordInColumn, 3);
				var ptrWordInOut4 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordInColumn, 4);
				var ptrWordInOut5 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordInColumn, 5);
				var ptrWordInOut6 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordInColumn, 6);
				var ptrWordInOut7 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordInColumn, 7);
				var ptrWordInOut8 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordInColumn, 8);
				var ptrWordInOut9 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordInColumn, 9);
				var ptrWordInOut10 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordInColumn, 10);
				var ptrWordInOut11 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordInColumn, 11);

				state[0] ^= ptrWordIn0 + ptrWordInOut0;
				state[1] ^= ptrWordIn1 + ptrWordInOut1;
				state[2] ^= ptrWordIn2 + ptrWordInOut2;
				state[3] ^= ptrWordIn3 + ptrWordInOut3;
				state[4] ^= ptrWordIn4 + ptrWordInOut4;
				state[5] ^= ptrWordIn5 + ptrWordInOut5;
				state[6] ^= ptrWordIn6 + ptrWordInOut6;
				state[7] ^= ptrWordIn7 + ptrWordInOut7;
				state[8] ^= ptrWordIn8 + ptrWordInOut8;
				state[9] ^= ptrWordIn9 + ptrWordInOut9;
				state[10] ^= ptrWordIn10 + ptrWordInOut10;
				state[11] ^= ptrWordIn11 + ptrWordInOut11;

				//Applies the reduced-round transformation f to the sponge's v_state
				Blake2bLyra(1);

				//M[row][C-1-col] = M[prev][col] XOR rand
				var ptrWordOutNewBlockData = new[]
				{
					ptrWordIn0 ^ state[0],
					ptrWordIn1 ^ state[1],
					ptrWordIn2 ^ state[2],
					ptrWordIn3 ^ state[3],
					ptrWordIn4 ^ state[4],
					ptrWordIn5 ^ state[5],
					ptrWordIn6 ^ state[6],
					ptrWordIn7 ^ state[7],
					ptrWordIn8 ^ state[8],
					ptrWordIn9 ^ state[9],
					ptrWordIn10 ^ state[10],
					ptrWordIn11 ^ state[11]
				};

				memoryMatrix.SetColumnWithBlockData(rowOut, ptrWordOutColumn, ptrWordOutNewBlockData);

				//M[row*][col] = M[row*][col] XOR rotW(rand)
				var ptrWordInOutNewBlockData = new[]
				{
					ptrWordInOut0 ^ state[11],
					ptrWordInOut1 ^ state[0],
					ptrWordInOut2 ^ state[1],
					ptrWordInOut3 ^ state[2],
					ptrWordInOut4 ^ state[3],
					ptrWordInOut5 ^ state[4],
					ptrWordInOut6 ^ state[5],
					ptrWordInOut7 ^ state[6],
					ptrWordInOut8 ^ state[7],
					ptrWordInOut9 ^ state[8],
					ptrWordInOut10 ^ state[9],
					ptrWordInOut11 ^ state[10]
				};

				memoryMatrix.SetColumnWithBlockData(rowInOut, ptrWordInColumn, ptrWordInOutNewBlockData);
			}
		}

		public void ReducedDuplexRow(ulong rowIn, ulong rowInOut, ulong rowOut,
			ulong nCols)
		{
			for (ulong i = 0; i < nCols; i++)
			{
				//Output: goes to previous column
				//Input: next column (i.e., next block in sequence)
				//INPUT AND OUTPUT COLUMN IS HERE THE SAME!
				ulong ptrWordColumn = i;

				//caching the values - for speed purposes and not duplicating the same code
				var ptrWordIn0 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordColumn, 0);
				var ptrWordIn1 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordColumn, 1);
				var ptrWordIn2 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordColumn, 2);
				var ptrWordIn3 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordColumn, 3);
				var ptrWordIn4 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordColumn, 4);
				var ptrWordIn5 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordColumn, 5);
				var ptrWordIn6 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordColumn, 6);
				var ptrWordIn7 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordColumn, 7);
				var ptrWordIn8 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordColumn, 8);
				var ptrWordIn9 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordColumn, 9);
				var ptrWordIn10 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordColumn, 10);
				var ptrWordIn11 = memoryMatrix.GetOneValueFromColumnBlockData(rowIn, ptrWordColumn, 11);

				var ptrWordInOut0 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 0);
				var ptrWordInOut1 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 1);
				var ptrWordInOut2 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 2);
				var ptrWordInOut3 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 3);
				var ptrWordInOut4 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 4);
				var ptrWordInOut5 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 5);
				var ptrWordInOut6 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 6);
				var ptrWordInOut7 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 7);
				var ptrWordInOut8 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 8);
				var ptrWordInOut9 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 9);
				var ptrWordInOut10 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 10);
				var ptrWordInOut11 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 11);

				state[0] ^= ptrWordIn0 + ptrWordInOut0;
				state[1] ^= ptrWordIn1 + ptrWordInOut1;
				state[2] ^= ptrWordIn2 + ptrWordInOut2;
				state[3] ^= ptrWordIn3 + ptrWordInOut3;
				state[4] ^= ptrWordIn4 + ptrWordInOut4;
				state[5] ^= ptrWordIn5 + ptrWordInOut5;
				state[6] ^= ptrWordIn6 + ptrWordInOut6;
				state[7] ^= ptrWordIn7 + ptrWordInOut7;
				state[8] ^= ptrWordIn8 + ptrWordInOut8;
				state[9] ^= ptrWordIn9 + ptrWordInOut9;
				state[10] ^= ptrWordIn10 + ptrWordInOut10;
				state[11] ^= ptrWordIn11 + ptrWordInOut11;

				Blake2bLyra(1);

				var ptrWordOutNewBlockData = new[]
				{
					//setting ptrWordOut
					memoryMatrix.GetOneValueFromColumnBlockData(rowOut, ptrWordColumn, 0) ^ state[0],
					memoryMatrix.GetOneValueFromColumnBlockData(rowOut, ptrWordColumn, 1) ^ state[1],
					memoryMatrix.GetOneValueFromColumnBlockData(rowOut, ptrWordColumn, 2) ^ state[2],
					memoryMatrix.GetOneValueFromColumnBlockData(rowOut, ptrWordColumn, 3) ^ state[3],
					memoryMatrix.GetOneValueFromColumnBlockData(rowOut, ptrWordColumn, 4) ^ state[4],
					memoryMatrix.GetOneValueFromColumnBlockData(rowOut, ptrWordColumn, 5) ^ state[5],
					memoryMatrix.GetOneValueFromColumnBlockData(rowOut, ptrWordColumn, 6) ^ state[6],
					memoryMatrix.GetOneValueFromColumnBlockData(rowOut, ptrWordColumn, 7) ^ state[7],
					memoryMatrix.GetOneValueFromColumnBlockData(rowOut, ptrWordColumn, 8) ^ state[8],
					memoryMatrix.GetOneValueFromColumnBlockData(rowOut, ptrWordColumn, 9) ^ state[9],
					memoryMatrix.GetOneValueFromColumnBlockData(rowOut, ptrWordColumn, 10) ^ state[10],
					memoryMatrix.GetOneValueFromColumnBlockData(rowOut, ptrWordColumn, 11) ^ state[11]
				};

				memoryMatrix.SetColumnWithBlockData(rowOut, ptrWordColumn, ptrWordOutNewBlockData);

				// Get new values in case that rowOut == rowInOut
				ptrWordInOut0 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 0);
				ptrWordInOut1 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 1);
				ptrWordInOut2 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 2);
				ptrWordInOut3 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 3);
				ptrWordInOut4 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 4);
				ptrWordInOut5 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 5);
				ptrWordInOut6 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 6);
				ptrWordInOut7 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 7);
				ptrWordInOut8 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 8);
				ptrWordInOut9 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 9);
				ptrWordInOut10 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 10);
				ptrWordInOut11 = memoryMatrix.GetOneValueFromColumnBlockData(rowInOut, ptrWordColumn, 11);

				var ptrWordInOutNewBlockData = new ulong[]
				{
					ptrWordInOut0 ^ state[11],
					ptrWordInOut1 ^ state[0],
					ptrWordInOut2 ^ state[1],
					ptrWordInOut3 ^ state[2],
					ptrWordInOut4 ^ state[3],
					ptrWordInOut5 ^ state[4],
					ptrWordInOut6 ^ state[5],
					ptrWordInOut7 ^ state[6],
					ptrWordInOut8 ^ state[7],
					ptrWordInOut9 ^ state[8],
					ptrWordInOut10 ^ state[9],
					ptrWordInOut11 ^ state[10]
				};

				memoryMatrix.SetColumnWithBlockData(rowInOut, ptrWordColumn, ptrWordInOutNewBlockData);
			}
		}

		public void AbsorbBlock(long rowa)
		{
			state[0] ^= memoryMatrix.GetOneValueFromColumnBlockData((ulong)rowa, 0, 0);
			state[1] ^= memoryMatrix.GetOneValueFromColumnBlockData((ulong)rowa, 0, 1);
			state[2] ^= memoryMatrix.GetOneValueFromColumnBlockData((ulong)rowa, 0, 2);
			state[3] ^= memoryMatrix.GetOneValueFromColumnBlockData((ulong)rowa, 0, 3);
			state[4] ^= memoryMatrix.GetOneValueFromColumnBlockData((ulong)rowa, 0, 4);
			state[5] ^= memoryMatrix.GetOneValueFromColumnBlockData((ulong)rowa, 0, 5);
			state[6] ^= memoryMatrix.GetOneValueFromColumnBlockData((ulong)rowa, 0, 6);
			state[7] ^= memoryMatrix.GetOneValueFromColumnBlockData((ulong)rowa, 0, 7);
			state[8] ^= memoryMatrix.GetOneValueFromColumnBlockData((ulong)rowa, 0, 8);
			state[9] ^= memoryMatrix.GetOneValueFromColumnBlockData((ulong)rowa, 0, 9);
			state[10] ^= memoryMatrix.GetOneValueFromColumnBlockData((ulong)rowa, 0, 10);
			state[11] ^= memoryMatrix.GetOneValueFromColumnBlockData((ulong)rowa, 0, 11);

			Blake2bLyra(12);
		}

		public void Squeeze(byte[] K)
		{
			var len = (ulong)K.Length;
			var fullBlocks = len / LyraConstants.BLOCK_LEN_BYTES;
			var ptr = 0;

			var v_state_bytes = state.SelectMany(BitConverter.GetBytes).ToArray();
			for (ulong i = 0; i < fullBlocks; i++)
			{
				Array.Copy(v_state_bytes, 0, K, ptr, (int)LyraConstants.BLOCK_LEN_BYTES);
				Blake2bLyra(12);
				ptr += (int)LyraConstants.BLOCK_LEN_BYTES;
			}

			Array.Copy(v_state_bytes, 0, K, ptr, (int)(len % LyraConstants.BLOCK_LEN_BYTES));
		}
	}
}