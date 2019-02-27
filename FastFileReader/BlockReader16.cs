using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FastFileReader
{
   partial class BlockReader16 : BlockReader
   {
      private bool bigEndian;

      public override long StreamLength => PositionFirstByte(streamLength);

      public BlockReader16(Stream stream, bool bigEndian) : base(stream)
      {
         this.bigEndian = bigEndian;
         ((Buffer16)FirstBuffer).BigEndian = bigEndian;
         ((Buffer16)SecondBuffer).BigEndian = bigEndian;
      }

      protected override Buffer CreateBuffer()
      {
         return new Buffer16();
      }

      static long PosFirstByte(long position) => (long)((ulong)position & 0xFFFFFFFFFFFFFFFE);

      public override int MinCodePointSize => 2;
      public override long PositionFirstByte(long position) => PosFirstByte(position);

      public override ISearchData CreateSearchData(IEnumerable<uint> values)
      {
         return Buffer16.CreateSearchData(values.Select(a => new ValueTuple<byte, byte>(
            (byte)((a >> 8) & 0xFF),
            (byte)(a & 0xFF))).ToArray());
      }

      public long FindForward(long start, ValueTuple<byte, byte> value)
      {
         return FindForward(start, new ValueTuple<byte, byte>[] { value });
      }

      public long FindForward(long start, ValueTuple<byte, byte>[] values)
      {
         return FindForward(start, Buffer16.CreateSearchData(values));
      }

      public override long FindForward(long start, ISearchData searchData)
      {
         return FindForwardInternal(start, searchData, BufferSearch16.Instance);
      }

      public long FindBackward(long start, ValueTuple<byte, byte> value)
      {
         return FindBackward(start, new ValueTuple<byte, byte>[] { value });
      }

      public long FindBackward(long start, ValueTuple<byte, byte>[] values)
      {
         return FindBackward(start, Buffer16.CreateSearchData(values));
      }

      public override long FindBackward(long start, ISearchData searchData)
      {
         return FindBackwardInternal(start, searchData, BufferSearch16.Instance);
      }
   }
}
