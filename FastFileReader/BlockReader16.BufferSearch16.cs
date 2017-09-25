using System;

namespace FastFileReader {
   partial class BlockReader16 {
      private class BufferSearch16 : IBufferSearch {
         public static BufferSearch16 Instance { get; private set; } = new BufferSearch16();
         public bool TryFindAnyForward(Buffer buffer, long pos, ISearchData searchData, out long foundAt) {
            if (!(buffer is Buffer16))
               throw new ArgumentException($"Type of '{nameof(buffer)}' should be '{typeof(Buffer16).Name}'");
            if (!(searchData is Buffer16.InternalSearchData))
               throw new ArgumentException($"Type of '{nameof(searchData)}' should be '{typeof(Buffer16.InternalSearchData).Name}'");

            return ((Buffer16)buffer).TryFindAnyUInt16Forward(pos, (Buffer16.InternalSearchData)searchData, out foundAt);
         }
         public bool TryFindAnyBackward(Buffer buffer, long pos, ISearchData searchData, out long foundAt) {
            if (!(buffer is Buffer16))
               throw new ArgumentException($"Type of '{nameof(buffer)}' should be '{typeof(Buffer16).Name}'");
            if (!(searchData is Buffer16.InternalSearchData))
               throw new ArgumentException($"Type of '{nameof(searchData)}' should be '{typeof(Buffer16.InternalSearchData).Name}'");

            return ((Buffer16)buffer).TryFindAnyUInt16Backward(pos, (Buffer16.InternalSearchData)searchData, out foundAt);
         }
      }
   }
}
