using System;

namespace FastFileReader {
   partial class BlockReader {
      private class BufferSearch : IBufferSearch {
         private BufferSearch() { }
         public static BufferSearch Instance { get; private set; } = new BufferSearch();
         public bool TryFindAnyForward(Buffer buffer, long pos, ISearchData searchData, out long foundAt) {
            if (!(searchData is Buffer.InternalSearchData))
               throw new ArgumentException($"Type of '{nameof(searchData)}' should be '{typeof(Buffer.InternalSearchData).Name}'");

            return buffer.TryFindAnyForward(pos, (Buffer.InternalSearchData)searchData, out foundAt);
         }
         public bool TryFindAnyBackward(Buffer buffer, long pos, ISearchData searchData, out long foundAt) {
            if (!(searchData is Buffer.InternalSearchData))
               throw new ArgumentException($"Type of '{nameof(searchData)}' should be '{typeof(Buffer.InternalSearchData).Name}'");

            return buffer.TryFindAnyBackward(pos, (Buffer.InternalSearchData)searchData, out foundAt);
         }
      }
   }
}
