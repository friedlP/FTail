namespace FastFileReader {
   public class RawLine {
      Line line;
      public string Content => line.Content;
      public long Begin => line.Begin;
      public long End => line.End;
      public byte[] Bytes { get; private set; }

      public RawLine(string content, long begin, long end, byte[] bytes) {
         line = new Line(content, begin, end);
         this.Bytes = bytes;
      }

      public static implicit operator Line(RawLine line) {
         return line?.line;
      }
   }
}
