using System;

namespace FastFileReader
{
   partial class BlockReader32
   {
      protected class Buffer32 : Buffer
      {
         public new class InternalSearchData : ISearchData
         {
            public bool[] checkB0;
            public bool[] checkB1;
            public bool[] checkB2;
            public bool[] checkB3;
            public uint[][] b0Values;
            public uint[][] b1Values;
            public uint[][] b2Values;
            public uint[][] b3Values;
         }

         bool bigEndian;

         public bool BigEndian {
            get => bigEndian;
            set => bigEndian = value;
         }

         public override uint ReadValue(long position)
         {
            if (begin <= position && end > position + 3)
            {
               if (bigEndian)
               {
                  return ((uint)byteBuffer[position - begin] << 24)
                     | ((uint)byteBuffer[position + 1 - begin] << 16)
                     | ((uint)byteBuffer[position + 2 - begin] << 8)
                     | byteBuffer[position + 3 - begin];
               }
               else
               {
                  return ((uint)byteBuffer[position + 3 - begin] << 24)
                     | ((uint)byteBuffer[position + 2 - begin] << 16)
                     | ((uint)byteBuffer[position + 1 - begin] << 8)
                     | byteBuffer[position - begin];
               }
            }
            else
            {
               throw new ArgumentOutOfRangeException(nameof(position));
            }
         }

         public override bool TryReadValue(long position, out uint value)
         {
            if (begin <= position && end > position + 3)
            {
               if (bigEndian)
               {
                  value = ((uint)byteBuffer[position - begin] << 24)
                     | ((uint)byteBuffer[position + 1 - begin] << 16)
                     | ((uint)byteBuffer[position + 2 - begin] << 8)
                     | byteBuffer[position + 3 - begin];
               }
               else
               {
                  value = ((uint)byteBuffer[position + 3 - begin] << 24)
                     | ((uint)byteBuffer[position + 2 - begin] << 16)
                     | ((uint)byteBuffer[position + 1 - begin] << 8)
                     | byteBuffer[position - begin];
               }
               return true;
            }
            else
            {
               value = 0;
               return false;
            }
         }

         public static InternalSearchData CreateSearchData(ValueTuple<byte, byte, byte, byte>[] values)
         {
            InternalSearchData sd = new InternalSearchData();
            sd.checkB0 = new bool[256];
            sd.checkB1 = new bool[256];
            sd.checkB2 = new bool[256];
            sd.checkB3 = new bool[256];

            sd.b0Values = new uint[256][];
            sd.b1Values = new uint[256][];
            sd.b2Values = new uint[256][];
            sd.b3Values = new uint[256][];

            int[] n0 = new int[256];
            int[] n1 = new int[256];
            int[] n2 = new int[256];
            int[] n3 = new int[256];

            for (int i = 0; i < values.Length; ++i)
            {
               var v = values[i];
               byte b0 = v.Item1;
               byte b1 = v.Item2;
               byte b2 = v.Item3;
               byte b3 = v.Item4;
               ++n0[b0];
               ++n1[b1];
               ++n2[b2];
               ++n3[b3];
               sd.checkB0[b0] = true;
               sd.checkB1[b1] = true;
               sd.checkB2[b2] = true;
               sd.checkB3[b3] = true;
            }
            for (int i = 0; i < values.Length; ++i)
            {
               var v = values[i];
               byte b0 = v.Item1;
               byte b1 = v.Item2;
               byte b2 = v.Item3;
               byte b3 = v.Item4;
               uint[] v0 = sd.b0Values[b0];
               if (v0 == null)
               {
                  v0 = new uint[n0[b0]];
                  sd.b0Values[b0] = v0;
               }
               uint[] v1 = sd.b1Values[b1];
               if (v1 == null)
               {
                  v1 = new uint[n1[b1]];
                  sd.b1Values[b1] = v1;
               }
               uint[] v2 = sd.b2Values[b2];
               if (v2 == null)
               {
                  v2 = new uint[n2[b2]];
                  sd.b2Values[b2] = v2;
               }
               uint[] v3 = sd.b3Values[b3];
               if (v3 == null)
               {
                  v3 = new uint[n3[b3]];
                  sd.b3Values[b3] = v3;
               }

               uint val = ((uint)b0 << 24) | ((uint)b1 << 16) | ((uint)b2 << 8) | (uint)b3;
               v0[--n0[b0]] = val;
               v1[--n1[b1]] = val;
               v2[--n2[b2]] = val;
               v3[--n3[b3]] = val;
            }
            return sd;
         }

         public bool TryFindAny4BytesForward(long pos, ValueTuple<byte, byte, byte, byte>[] values, out long foundAt)
         {
            InternalSearchData sd = CreateSearchData(values);
            return TryFindAnyUInt32Forward(pos, sd, out foundAt);
         }

         public unsafe bool TryFindAnyUInt32Forward(long pos, InternalSearchData sd, out long foundAt)
         {
            foundAt = -1;

            pos = BlockReader32.PosFirstByte(pos);

            long curIdx = pos - begin;
            long length = end - begin;

            fixed (byte* buffer = byteBuffer)
            {
               fixed (bool* chk0 = sd.checkB0, chk1 = sd.checkB1, chk2 = sd.checkB2, chk3 = sd.checkB3)
               {
                  byte* bEnd = (buffer + length) - 3;
                  byte* bCur = buffer + curIdx;

                  if (bigEndian)
                  {
                     while (bCur < bEnd)
                     {
                        if ((*(chk3 + *(bCur + 3))) && (*(chk2 + *(bCur + 2))) && (*(chk1 + *(bCur + 1))) && (*(chk0 + *(bCur))))
                        {
                           if (CheckBytes(*bCur, *(bCur + 1), *(bCur + 2), *(bCur + 3), sd.b0Values, sd.b1Values, sd.b2Values, sd.b3Values))
                           {
                              foundAt = begin + (bCur - buffer);
                              return true;
                           }
                        }
                        bCur += 4;
                     }
                  }
                  else
                  {
                     while (bCur < bEnd)
                     {
                        if ((*(chk0 + *(bCur))) && (*(chk1 + *(bCur + 1))) && (*(chk2 + *(bCur + 2))) && (*(chk3 + *(bCur + 3))))
                        {
                           if (CheckBytes(*bCur, *(bCur + 1), *(bCur + 2), *(bCur + 3), sd.b0Values, sd.b1Values, sd.b2Values, sd.b3Values))
                           {
                              foundAt = begin + (bCur - buffer);
                              return true;
                           }
                        }
                        bCur += 4;
                     }
                  }
               }
            }

            return false;
         }

         public bool TryFindAny4BytesBackward(long pos, ValueTuple<byte, byte, byte, byte>[] values, out long foundAt)
         {
            InternalSearchData sd = CreateSearchData(values);
            return TryFindAnyUInt32Backward(pos, sd, out foundAt);
         }

         public unsafe bool TryFindAnyUInt32Backward(long pos, InternalSearchData sd, out long foundAt)
         {
            foundAt = -1;

            pos = BlockReader32.PosFirstByte(pos);

            long curIdx = pos - begin;
            long length = end - begin;

            fixed (byte* buffer = byteBuffer)
            {
               fixed (bool* chk0 = sd.checkB0, chk1 = sd.checkB1, chk2 = sd.checkB2, chk3 = sd.checkB3)
               {
                  byte* bCur = buffer + curIdx;

                  if (bigEndian)
                  {
                     while (bCur >= buffer)
                     {
                        if ((*(chk3 + *(bCur + 3))) && (*(chk2 + *(bCur + 2))) && (*(chk1 + *(bCur + 1))) && (*(chk0 + *(bCur))))
                        {
                           if (CheckBytes(*bCur, *(bCur + 1), *(bCur + 2), *(bCur + 3), sd.b0Values, sd.b1Values, sd.b2Values, sd.b3Values))
                           {
                              foundAt = begin + (bCur - buffer);
                              return true;
                           }
                        }
                        bCur -= 4;
                     }
                  }
                  else
                  {
                     while (bCur >= buffer)
                     {
                        if ((*(chk0 + *(bCur))) && (*(chk1 + *(bCur + 1))) && (*(chk2 + *(bCur + 2))) && (*(chk3 + *(bCur + 3))))
                        {
                           if (CheckBytes(*bCur, *(bCur + 1), *(bCur + 2), *(bCur + 3), sd.b0Values, sd.b1Values, sd.b2Values, sd.b3Values))
                           {
                              foundAt = begin + (bCur - buffer);
                              return true;
                           }
                        }
                        bCur -= 4;
                     }
                  }
               }
            }

            return false;
         }

         private bool CheckBytes(byte b0, byte b1, byte b2, byte b3, uint[][] b0Values, uint[][] b1Values, uint[][] b2Values, uint[][] b3Values)
         {
            uint u = (uint)((b0 << 24) | (b1 << 16) | (b2 << 8) | b3);
            uint[] arr = b0Values[b0];
            if (b1Values[b1].Length < arr.Length)
               arr = b1Values[b1];
            if (b2Values[b2].Length < arr.Length)
               arr = b2Values[b2];
            if (b3Values[b3].Length < arr.Length)
               arr = b3Values[b3];
            int len = arr.Length;
            for (int i = 0; i < len; ++i)
            {
               if (arr[i] == u)
                  return true;
            }
            return false;
         }
      }

   }
}
