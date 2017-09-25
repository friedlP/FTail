using System;

namespace FastFileReader {
   partial class BlockReader {
      protected class Buffer {
         public class InternalSearchData : ISearchData {
            public bool[] checkB0;
         }

         protected byte[] byteBuffer;
         protected long begin;
         protected long end;

         public byte[] ByteBuffer {
            get {
               return byteBuffer;
            }
         }
         public long Begin {
            get {
               return begin;
            }
            set {
               begin = value;
            }
         }
         public long End {
            get {
               return end;
            }
            set {
               end = value;
            }
         }

         public Buffer() {
            byteBuffer = new byte[32 * 1024];   // Always use multiple of 4 as size!
            begin = -1;
            end = -1;
         }

         public bool Contains(long position) {
            return end > begin && begin <= position && end > position;
         }
         
         public virtual uint ReadValue(long position) {
            if (begin <= position && end > position) {
               return byteBuffer[position - begin];
            } else {
               throw new ArgumentOutOfRangeException(nameof(position));
            }
         }

         public virtual bool TryReadValue(long position, out uint value) {
            if (begin <= position && end > position) {
               value = byteBuffer[position - begin];
               return true;
            } else {
               value = 0;
               return false;
            }
         }

         public bool Empty {
            get {
               return end - begin <= 0;
            }
         }

         public long Dist(long position) {
            if (position < begin) {
               return begin - position;
            } else if (position >= end) {
               return position - end + 1;
            } else {
               return 0;
            }
         }
         
         public static InternalSearchData CreateSearchData(byte[] values) {
            InternalSearchData sd = new InternalSearchData();
            sd.checkB0 = new bool[256];
            for (int i = 0; i < values.Length; ++i) {
               sd.checkB0[values[i]] = true;
            }
            return sd;
         }

         public bool TryFindAnyForward(long pos, byte[] values, out long foundAt) {
            return TryFindAnyForward(pos, CreateSearchData(values), out foundAt);
         }

         public bool TryFindAnyBackward(long pos, byte[] values, out long foundAt) {
            return TryFindAnyBackward(pos, CreateSearchData(values), out foundAt);
         }

         public unsafe bool TryFindAnyForward(long pos, InternalSearchData sd, out long foundAt) {
            foundAt = -1;
            long curIdx = pos - begin;
            long length = end - begin;
            if (curIdx < 0 || curIdx >= length)
               return false;
            if (begin < 0 || length < 0)
               return false;
            if (sd.checkB0 == null || sd.checkB0.Length != 256)
               return false;
            if (byteBuffer == null || byteBuffer.Length < length)
               return false;

            fixed (byte* buffer = byteBuffer) {
               fixed (bool* chk = sd.checkB0) {
                  byte* bEnd = buffer + length;
                  byte* bCur = buffer + curIdx;
                  while (bCur < bEnd) {
                     if (*(chk + *(bCur))) {
                        foundAt = begin + (bCur - buffer);
                        return true;
                     }
                     ++bCur;
                  }
               }
            }
            return false;
         }

         public unsafe bool TryFindAnyBackward(long pos, InternalSearchData sd, out long foundAt) {
            foundAt = -1;
            long curIdx = pos - begin;
            long length = end - begin;
            if (curIdx < 0 || curIdx >= length)
               return false;
            if (begin < 0 || length < 0)
               return false;
            if (sd.checkB0 == null || sd.checkB0.Length != 256)
               return false;
            if (byteBuffer == null || byteBuffer.Length < length)
               return false;
            
            fixed (byte* buffer = byteBuffer) {
               fixed (bool* chk = sd.checkB0) {
                  byte* bCur = buffer + curIdx;
                  while (bCur >= buffer) {
                     if (*(chk + *(bCur))) {
                        foundAt = begin + (bCur - buffer);
                        return true;
                     }
                     --bCur;
                  }
               }
            }

            return false;
         }

         public void ReadRange(long beginPos, long endPos, byte[] buffer, int bufferPos) {
            if (begin < 0 || end < 0)
               throw new InvalidOperationException("Internal buffer has not been initialized yet");
            if (beginPos < begin)
               throw new ArgumentOutOfRangeException(nameof(beginPos));
            if (endPos > end)
               throw new ArgumentOutOfRangeException(nameof(endPos));

            int idx = (int)(beginPos - begin);
            int count = (int)(endPos - beginPos);
            Array.Copy(byteBuffer, idx, buffer, bufferPos, count);
         }
      }
   }
}
