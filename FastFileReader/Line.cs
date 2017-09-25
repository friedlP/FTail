namespace FastFileReader {
   public class Line {
      public string Content { get; private set; }
      public long Begin { get; private set; }
      public long End { get; private set; }

      public Line(string content, long begin, long end) {
         this.Content = content;
         this.Begin = begin;
         this.End = end;
      }
   }
}
