using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FastFileReader {
   partial class BlockReader32 : BlockReader16 {
      private bool bigEndian;

      public override long StreamLength => PositionFirstByte(streamLength);

      public BlockReader32(Stream stream, bool bigEndian) : base(stream, bigEndian) {
         this.bigEndian = bigEndian;
         ((Buffer32)FirstBuffer).BigEndian = bigEndian;
         ((Buffer32)SecondBuffer).BigEndian = bigEndian;
      }

      protected override Buffer CreateBuffer() {
         return new Buffer32();
      }

      static long PosFirstByte(long position) => (long)((ulong)position & 0xFFFFFFFFFFFFFFFC);

      public override int MinCodePointSize => 4;
      public override long PositionFirstByte(long position) => PosFirstByte(position);

      public override ISearchData CreateSearchData(IEnumerable<uint> values) {
         return Buffer32.CreateSearchData(values.Select(a => new ValueTuple<byte, byte, byte, byte>(
            (byte)((a >> 24) & 0xFF),
            (byte)((a >> 16) & 0xFF),
            (byte)((a >> 8) & 0xFF),
            (byte)(a & 0xFF))).ToArray());
      }

      public long FindForward(long start, ValueTuple<byte, byte, byte, byte> value) {
         return FindForward(start, new ValueTuple<byte, byte, byte, byte>[] { value });
      }

      public long FindForward(long start, ValueTuple<byte, byte, byte, byte>[] values) {
         return FindForward(start, Buffer32.CreateSearchData(values));
      }

      public override long FindForward(long start, ISearchData searchData) {
         return FindForwardInternal(start, searchData, BufferSearch32.Instance);
      }

      public long FindBackward(long start, ValueTuple<byte, byte, byte, byte> value) {
         return FindBackward(start, new ValueTuple<byte, byte, byte, byte>[] { value });
      }

      public long FindBackward(long start, ValueTuple<byte, byte, byte, byte>[] values) {
         return FindBackward(start, Buffer32.CreateSearchData(values));
      }

      public override long FindBackward(long start, ISearchData searchData) {
         return FindBackwardInternal(start, searchData, BufferSearch32.Instance);
      }
   }
}
