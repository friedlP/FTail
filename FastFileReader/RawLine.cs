namespace FastFileReader {
   public class RawLine {
      Line line;
      public string Content => line.Content;
      public Extent Extent => line.Extent;
      public long Begin => Extent.Begin;
      public long End => Extent.End;
      public byte[] Bytes { get; private set; }

      public RawLine(string content, Extent extent, byte[] bytes) {
         line = new Line(content, extent);
         this.Bytes = bytes;
      }

      public static implicit operator Line(RawLine line) {
         return line?.line;
      }
   }
}
