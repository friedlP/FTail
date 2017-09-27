namespace FastFileReader {
   public class Line {
      public string Content { get; private set; }
      public long Begin { get; private set; }
      public long End { get; private set; }
      public byte[] Bytes { get; private set; }

      public Line(string content, long begin, long end, byte[] bytes) {
         this.Content = content;
         this.Begin = begin;
         this.End = end;
         this.Bytes = bytes;
      }
   }
}
