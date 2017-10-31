using System.Collections.Generic;

namespace FastFileReader {
   public class LineRange {
      public LineRange(Line requestedLine, List<Line> previousLines, List<Line> nextLines, 
                       List<Extent> previousExtents, List<Extent> nextExtents, long streamLength) {
         RequestedLine = requestedLine;
         PreviousLines = previousLines ?? new List<Line>();
         NextLines = nextLines ?? new List<Line>();
         PreviousExtents = previousExtents ?? new List<Extent>();
         NextExtents = nextExtents ?? new List<Extent>();
         StreamLength = streamLength;
      }

      public LineRange() {
         RequestedLine = null;
         PreviousLines = new List<Line>();
         NextLines = new List<Line>();
         PreviousExtents = new List<Extent>();
         NextExtents = new List<Extent>();
         StreamLength = -1;
      }

      public List<Extent> PreviousExtents { get; private set; }
      public List<Line> PreviousLines { get; private set; }
      public Line RequestedLine { get; private set; }
      public List<Line> NextLines { get; private set; }
      public List<Extent> NextExtents { get; private set; }
      public long StreamLength { get; private set; }

      public override bool Equals(object obj) {
         var range = obj as LineRange;
         return range != null &&
            this == range;
      }

      public IEnumerable<Line> AllLines {
         get {
            foreach (Line line in PreviousLines)
               yield return line;
            if (RequestedLine != null)
               yield return RequestedLine;
            foreach (Line line in NextLines)
               yield return line;
         }
      }

      public Line FirstLine {
         get {
            if (PreviousLines.Count > 0)
               return PreviousLines[0];
            if (RequestedLine != null)
               return RequestedLine;
            if (NextLines.Count > 0)
               return NextLines[0];
            return null;
         }
      }

      public Line LastLine {
         get {
            if (NextLines.Count > 0)
               return NextLines[NextLines.Count - 1];
            if (RequestedLine != null)
               return RequestedLine;
            if (PreviousLines.Count > 0)
               return PreviousLines[PreviousLines.Count - 1];
            return null;
         }
      }

      public Extent FirstExtent {
         get {
            if (PreviousExtents.Count > 0)
               return PreviousExtents[0];
            Line firstLine = FirstLine;
            if (firstLine != null)
               return firstLine.Extent;
            if (NextExtents.Count > 0)
               return NextExtents[0];
            return null;
         }
      }

      public Extent LastExtent {
         get {
            if (NextExtents.Count > 0)
               return NextExtents[NextExtents.Count - 1];
            Line lastLine = LastLine;
            if (lastLine != null)
               return lastLine.Extent;
            if (PreviousExtents.Count > 0)
               return PreviousExtents[PreviousExtents.Count - 1];
            return null;
         }
      }

      public IEnumerable<Extent> AllExtents {
         get {
            foreach (Extent extent in PreviousExtents)
               yield return extent;
            foreach (Line line in AllLines)
               yield return line.Extent;
            foreach (Extent extent in NextExtents)
               yield return extent;
         }
      }

      public override int GetHashCode() {
         var hashCode = 387268372;
         hashCode = hashCode * -1521134295 + RequestedLine?.GetHashCode() ?? 387268372;
         hashCode = hashCode * -1521134295 + StreamLength.GetHashCode();
         hashCode = hashCode * -1521134295 + HashCode(PreviousLines);
         hashCode = hashCode * -1521134295 + HashCode(NextLines);
         hashCode = hashCode * -1521134295 + HashCode(PreviousExtents);
         hashCode = hashCode * -1521134295 + HashCode(NextExtents);

         return hashCode;
      }

      private int HashCode<T>(List<T> list) {
         var hashCode = 387268372;
         if (list != null) {
            int count = list.Count;
            hashCode = hashCode * -1521134295 + count.GetHashCode();
            if (count > 0) {
               // First entry
               hashCode = hashCode * -1521134295 + list[0]?.GetHashCode() ?? 387268372;
            }
            if (count > 1) {
               // Last entry
               hashCode = hashCode * -1521134295 + list[count - 1]?.GetHashCode() ?? 387268372;
            }
         }
         return hashCode;
      }

      public static bool operator ==(LineRange lhs, LineRange rhs) {
         if (System.Object.ReferenceEquals(lhs, rhs)) {
            return true;
         }

         // If one is null, but not both, return false.
         if (((object)lhs == null) || ((object)rhs == null)) {
            return false;
         }

         if (lhs.StreamLength != rhs.StreamLength)
            return false;

         if (lhs.PreviousLines.Count != rhs.PreviousLines.Count)
            return false;

         if (lhs.NextLines.Count != rhs.NextLines.Count)
            return false;

         if (lhs.PreviousExtents.Count != rhs.PreviousExtents.Count)
            return false;

         if (lhs.NextExtents.Count != rhs.NextExtents.Count)
            return false;

         if (lhs.RequestedLine != rhs.RequestedLine)
            return false;

         for (int i = 0; i < lhs.PreviousLines.Count; ++i) {
            if (lhs.PreviousLines[i] != rhs.PreviousLines[i])
               return false;
         }

         for (int i = 0; i < lhs.NextLines.Count; ++i) {
            if (lhs.NextLines[i] != rhs.NextLines[i])
               return false;
         }
         
         for (int i = 0; i < lhs.PreviousExtents.Count; ++i) {
            if (lhs.PreviousExtents[i] != rhs.PreviousExtents[i])
               return false;
         }

         for (int i = 0; i < lhs.NextExtents.Count; ++i) {
            if (lhs.NextExtents[i] != rhs.NextExtents[i])
               return false;
         }

         return true;
      }

      public static bool operator !=(LineRange lhs, LineRange rhs) {
         return !(lhs == rhs);
      }
   }
}
