using System;

namespace FastFileReader
{
   partial class BlockReader16
   {
      protected class Buffer16 : Buffer
      {
         public new class InternalSearchData : ISearchData
         {
            public bool[] checkB0;
            public bool[] checkB1;
            public ushort[][] b0Values;
            public ushort[][] b1Values;
         }

         bool bigEndian;

         public bool BigEndian {
            get => bigEndian;
            set => bigEndian = value;
         }

         public override uint ReadValue(long position)
         {
            if (begin <= position && end > position + 1)
            {
               if (bigEndian)
               {
                  return ((uint)byteBuffer[position - begin] << 8) | byteBuffer[position + 1 - begin];
               }
               else
               {
                  return ((uint)byteBuffer[position + 1 - begin] << 8) | byteBuffer[position - begin];
               }
            }
            else
            {
               throw new ArgumentOutOfRangeException(nameof(position));
            }
         }

         public override bool TryReadValue(long position, out uint value)
         {
            if (begin <= position && end > position + 1)
            {
               if (bigEndian)
               {
                  value = ((uint)byteBuffer[position - begin] << 8) | byteBuffer[position + 1 - begin];
               }
               else
               {
                  value = ((uint)byteBuffer[position + 1 - begin] << 8) | byteBuffer[position - begin];
               }
               return true;
            }
            else
            {
               value = 0;
               return false;
            }
         }

         public static InternalSearchData CreateSearchData(ValueTuple<byte, byte>[] values)
         {
            InternalSearchData sd = new InternalSearchData();
            sd.checkB0 = new bool[256];
            sd.checkB1 = new bool[256];

            sd.b0Values = new ushort[256][];
            sd.b1Values = new ushort[256][];

            int[] n0 = new int[256];
            int[] n1 = new int[256];

            for (int i = 0; i < values.Length; ++i)
            {
               var v = values[i];
               byte b0 = v.Item1;
               byte b1 = v.Item2;
               ++n0[b0];
               ++n1[b1];
               sd.checkB0[b0] = true;
               sd.checkB1[b1] = true;
            }
            for (int i = 0; i < values.Length; ++i)
            {
               var v = values[i];
               byte b0 = v.Item1;
               byte b1 = v.Item2;
               ushort[] v0 = sd.b0Values[b0];
               if (v0 == null)
               {
                  v0 = new ushort[n0[b0]];
                  sd.b0Values[b0] = v0;
               }
               ushort[] v1 = sd.b1Values[b1];
               if (v1 == null)
               {
                  v1 = new ushort[n1[b1]];
                  sd.b1Values[b1] = v1;
               }

               ushort val = (ushort)((b0 << 8) | b1);
               v0[--n0[b0]] = val;
               v1[--n1[b1]] = val;
            }
            return sd;
         }

         public bool TryFindAny2BytesForward(long pos, ValueTuple<byte, byte>[] values, out long foundAt)
         {
            InternalSearchData sd = CreateSearchData(values);
            return TryFindAnyUInt16Forward(pos, sd, out foundAt);
         }

         public unsafe bool TryFindAnyUInt16Forward(long pos, InternalSearchData sd, out long foundAt)
         {
            foundAt = -1;

            pos = BlockReader16.PosFirstByte(pos);

            long curIdx = pos - begin;
            long length = end - begin;

            fixed (byte* buffer = byteBuffer)
            {
               fixed (bool* chk0 = sd.checkB0, chk1 = sd.checkB1)
               {
                  byte* bEnd = (buffer + length) - 1;
                  byte* bCur = buffer + curIdx;

                  if (bigEndian)
                  {
                     while (bCur < bEnd)
                     {
                        if ((*(chk1 + *(bCur + 1))) && (*(chk0 + *(bCur))))
                        {
                           if (CheckBytes(*bCur, *(bCur + 1), sd.b0Values, sd.b1Values))
                           {
                              foundAt = begin + (bCur - buffer);
                              return true;
                           }
                        }
                        bCur += 2;
                     }
                  }
                  else
                  {
                     while (bCur < bEnd)
                     {
                        if ((*(chk0 + *(bCur))) && (*(chk1 + *(bCur + 1))))
                        {
                           if (CheckBytes(*bCur, *(bCur + 1), sd.b0Values, sd.b1Values))
                           {
                              foundAt = begin + (bCur - buffer);
                              return true;
                           }
                        }
                        bCur += 2;
                     }
                  }
               }
            }

            return false;
         }

         public bool TryFindAny2BytesBackward(long pos, ValueTuple<byte, byte>[] values, out long foundAt)
         {
            InternalSearchData sd = CreateSearchData(values);
            return TryFindAnyUInt16Backward(pos, sd, out foundAt);
         }

         public unsafe bool TryFindAnyUInt16Backward(long pos, InternalSearchData sd, out long foundAt)
         {
            foundAt = -1;

            pos = BlockReader16.PosFirstByte(pos);

            long curIdx = pos - begin;
            long length = end - begin;

            fixed (byte* buffer = byteBuffer)
            {
               fixed (bool* chk0 = sd.checkB0, chk1 = sd.checkB1)
               {
                  byte* bCur = buffer + curIdx;

                  if (bigEndian)
                  {
                     while (bCur >= buffer)
                     {
                        if ((*(chk1 + *(bCur + 1))) && (*(chk0 + *(bCur))))
                        {
                           if (CheckBytes(*bCur, *(bCur + 1), sd.b0Values, sd.b1Values))
                           {
                              foundAt = begin + (bCur - buffer);
                              return true;
                           }
                        }
                        bCur -= 2;
                     }
                  }
                  else
                  {
                     while (bCur >= buffer)
                     {
                        if ((*(chk0 + *(bCur))) && (*(chk1 + *(bCur + 1))))
                        {
                           if (CheckBytes(*bCur, *(bCur + 1), sd.b0Values, sd.b1Values))
                           {
                              foundAt = begin + (bCur - buffer);
                              return true;
                           }
                        }
                        bCur -= 2;
                     }
                  }
               }
            }

            return false;
         }

         private bool CheckBytes(byte b0, byte b1, ushort[][] b0Values, ushort[][] b1Values)
         {
            ushort u = (ushort)((b0 << 8) | b1);
            ushort[] arr = b0Values[b0].Length <= b1Values[b1].Length ? b0Values[b0] : b1Values[b1];
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
