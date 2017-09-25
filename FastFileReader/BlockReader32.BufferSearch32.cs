using System;

namespace FastFileReader {
   partial class BlockReader32 {
      private class BufferSearch32 : IBufferSearch {
         public static BufferSearch32 Instance { get; private set; } = new BufferSearch32();
         public bool TryFindAnyForward(Buffer buffer, long pos, ISearchData searchData, out long foundAt) {
            if (!(buffer is Buffer32))
               throw new ArgumentException($"Type of '{nameof(buffer)}' should be '{typeof(Buffer32).Name}'");
            if (!(searchData is Buffer32.InternalSearchData))
               throw new ArgumentException($"Type of '{nameof(searchData)}' should be '{typeof(Buffer32.InternalSearchData).Name}'");

            return ((Buffer32)buffer).TryFindAnyUInt32Forward(pos, (Buffer32.InternalSearchData)searchData, out foundAt);
         }
         public bool TryFindAnyBackward(Buffer buffer, long pos, ISearchData searchData, out long foundAt) {
            if (!(buffer is Buffer32))
               throw new ArgumentException($"Type of '{nameof(buffer)}' should be '{typeof(Buffer32).Name}'");
            if (!(searchData is Buffer32.InternalSearchData))
               throw new ArgumentException($"Type of '{nameof(searchData)}' should be '{typeof(Buffer32.InternalSearchData).Name}'");

            return ((Buffer32)buffer).TryFindAnyUInt32Backward(pos, (Buffer32.InternalSearchData)searchData, out foundAt);
         }
      }
   }
}
