﻿using System.Collections.Generic;

namespace FastFileReader {
   public class LineRange {
      public LineRange(Line requestedLine, List<Line> previousLines, List<Line> nextLines, long streamLength) {
         RequestedLine = requestedLine;
         PreviousLines = previousLines ?? new List<Line>();
         NextLines = nextLines ?? new List<Line>();
         StreamLength = streamLength;
      }

      public List<Line> PreviousLines { get; private set; }
      public Line RequestedLine { get; private set; }
      public List<Line> NextLines { get; private set; }
      public long StreamLength { get; private set; }

      public override bool Equals(object obj) {
         var range = obj as LineRange;
         return range != null &&
                EqualityComparer<List<Line>>.Default.Equals(PreviousLines, range.PreviousLines) &&
                EqualityComparer<Line>.Default.Equals(RequestedLine, range.RequestedLine) &&
                EqualityComparer<List<Line>>.Default.Equals(NextLines, range.NextLines) &&
                StreamLength == range.StreamLength;
      }

      public override int GetHashCode() {
         var hashCode = 387268372;
         hashCode = hashCode * -1521134295 + RequestedLine?.GetHashCode() ?? 387268372;
         hashCode = hashCode * -1521134295 + StreamLength.GetHashCode();
         hashCode = hashCode * -1521134295 + HashCode(PreviousLines);
         hashCode = hashCode * -1521134295 + HashCode(NextLines);

         return hashCode;
      }

      private int HashCode(List<Line> lineList) {
         var hashCode = 387268372;
         if (lineList != null) {
            hashCode = hashCode * -1521134295;
            foreach (Line l in lineList) {
               hashCode = hashCode * -1521134295 + l?.GetHashCode() ?? 387268372;
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

         if (lhs.RequestedLine != rhs.RequestedLine)
            return false;

         for (int i = 0; i < lhs.PreviousLines.Count; ++i) {
            if (lhs.PreviousLines[i] != rhs.PreviousLines[i])
               return false;
         }

         for (int i = 0; i < lhs.PreviousLines.Count; ++i) {
            if (lhs.PreviousLines[i] != rhs.PreviousLines[i])
               return false;
         }

         for (int i = 0; i < lhs.PreviousLines.Count; ++i) {
            if (lhs.NextLines[i] != rhs.NextLines[i])
               return false;
         }

         return true;
      }

      public static bool operator !=(LineRange lhs, LineRange rhs) {
         return !(lhs == rhs);
      }
   }
}