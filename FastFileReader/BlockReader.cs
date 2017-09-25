using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FastFileReader {
   interface ISearchData {
   }

   partial class BlockReader {
      Stream stream;
      Buffer firstBuffer;
      Buffer secondBuffer;
      protected long streamLength;

      public BlockReader(Stream stream) {
         if (!stream.CanRead)
            throw new ArgumentException("Stream does not support reading.");
         if (!stream.CanSeek)
            throw new ArgumentException("Stream does not support seeking.");

         this.stream = stream;
         streamLength = stream.Length;
         firstBuffer = CreateBuffer();
         secondBuffer = CreateBuffer();
      }
      
      protected virtual Buffer CreateBuffer() {
         return new Buffer();
      }

      protected Buffer FirstBuffer => firstBuffer;
      protected Buffer SecondBuffer => secondBuffer;

      public virtual long StreamLength => streamLength;

      public virtual ISearchData CreateSearchData(IEnumerable<uint> values) {
         return Buffer.CreateSearchData(values.Select(a => (byte)(a & 0xFF)).ToArray());
      }

      public long FindForward(long start, byte value) {
         return FindForward(start, new byte[] { value });
      }

      public long FindForward(long start, byte[] values) {
         return FindForward(start, Buffer.CreateSearchData(values));
      }

      public virtual long FindForward(long start, ISearchData searchData) {
         return FindForwardInternal(start, searchData, BufferSearch.Instance);
      }

      protected long FindForwardInternal(long start, ISearchData searchData, IBufferSearch bufferSearch) {
         if (start < 0)
            return -1;

         long pos = start;
         while (pos < StreamLength) {
            Buffer b = GetContainingBuffer(pos);
            if (b != null) {
               if (bufferSearch.TryFindAnyForward(b, pos, searchData, out long foundAt)) {
                  return foundAt;
               }
               pos = b.End;
            } else {
               FillBuffer(pos);
            }
         }
         return -1;
      }

      public long FindBackward(long start, byte value) {
         return FindBackward(start, new byte[] { value });
      }

      public long FindBackward(long start, byte[] values) {
         return FindBackward(start, Buffer.CreateSearchData(values));
      }

      public virtual long FindBackward(long start, ISearchData searchData) {
         return FindBackwardInternal(start, searchData, BufferSearch.Instance);
      }

      protected long FindBackwardInternal(long start, ISearchData searchData, IBufferSearch bufferSearch) {
         if (start >= StreamLength)
            return -1;
         
         long pos = start;
         while (pos >= 0) {
            Buffer b = GetContainingBuffer(pos);
            if (b != null) {
               if (bufferSearch.TryFindAnyBackward(b, pos, searchData, out long foundAt)) {
                  return foundAt;
               }
               pos = b.Begin - 1;
            } else {
               FillBuffer(pos);
            }
         }
         return -1;
      }

      protected Buffer GetContainingBuffer(long pos) {
         if (firstBuffer.Contains(pos))
            return firstBuffer;
         if (secondBuffer.Contains(pos))
            return secondBuffer;
         return null;
      }

      protected void FillBuffer(long pos) {
         if (firstBuffer.Empty) {
            FillBuffer(firstBuffer, secondBuffer, pos);
         } else if (secondBuffer.Empty) {
            FillBuffer(secondBuffer, firstBuffer, pos);
            SwitchBuffer();
         } else {
            long distA = firstBuffer.Dist(pos);
            long distB = secondBuffer.Dist(pos);
            if (distA > distB) {
               FillBuffer(firstBuffer, secondBuffer, pos);
            } else {
               FillBuffer(secondBuffer, firstBuffer, pos);
               SwitchBuffer();
            }
         }
      }

      public byte[] ReadRange(long begin, long end) {
         if (begin < 0)
            throw new ArgumentOutOfRangeException(nameof(begin));
         if (end > StreamLength)
            throw new ArgumentOutOfRangeException(nameof(end));
         if (end < begin)
            throw new ArgumentException($"Value of parameter {nameof(end)} must not be less than value of parameter {nameof(begin)}");

         byte[] buffer = new byte[end - begin];
         int bufferPos = 0;
         long filePos = begin;
         while (filePos < end) {
            Buffer b = GetContainingBuffer(filePos);
            if (b != null) {
               long endPos = b.End;
               if (endPos > end) {
                  endPos = end;
               }
               b.ReadRange(filePos, endPos, buffer, bufferPos);
               bufferPos += (int)(endPos - filePos);
               filePos = endPos;
            } else {
               FillBuffer(filePos);
            }
         }
         return buffer;
      }

      public virtual int MinCodePointSize => 1;
      public virtual long PositionFirstByte(long position) => position;

      public uint ReadValue(long position) {
         uint value;
         if (firstBuffer.TryReadValue(position, out value)) {
            return value;
         }
         if (secondBuffer.TryReadValue(position, out value)) {
            SwitchBuffer();
            return value;
         }

         FillBuffer(position);
         return firstBuffer.ReadValue(position);
      }

      private void SwitchBuffer() {
         Buffer temp = firstBuffer;
         firstBuffer = secondBuffer;
         secondBuffer = temp;
      }

      private void FillBuffer(Buffer buffer, Buffer otherBuffer, long position) {
         long begin;
         long end;
         begin = position - (buffer.ByteBuffer.Length / 2);

         // Align to 4 bytes
         begin = (long)((ulong)begin & 0xFFFFFFFFFFFFFFFE);

         if (begin < 0)
            begin = 0;
         if (!otherBuffer.Empty) {
            if (otherBuffer.Begin <= begin && otherBuffer.End > begin) {
               // Begin would be within the other buffer
               // --> set begin to the end of the other buffer
               begin = otherBuffer.End;
            }
         }
         end = begin + buffer.ByteBuffer.Length;
         if (end > StreamLength)
            end = StreamLength;
         if (!otherBuffer.Empty) {
            if (otherBuffer.Begin <= end && otherBuffer.End > end) {
               // End would be within the other buffer
               // --> set end to the begin of the other buffer and also update begin
               end = otherBuffer.Begin;
               begin = end - buffer.ByteBuffer.Length;
               if (begin < 0)
                  begin = 0;
            }
         }

         buffer.Begin = begin;
         buffer.End = end;
         if (end > begin) {
            stream.Seek(begin, SeekOrigin.Begin);
            stream.Read(buffer.ByteBuffer, 0, (int)(end - begin));
         }
      }

      protected interface IBufferSearch {
         bool TryFindAnyForward(Buffer buffer, long pos, ISearchData sd, out long foundAt);
         bool TryFindAnyBackward(Buffer buffer, long pos, ISearchData sd, out long foundAt);
      }

      protected enum Direction {
         forward,
         backward
      }
   }
}
