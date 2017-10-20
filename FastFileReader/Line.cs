using System.Collections.Generic;

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

      public static bool operator ==(Line lhs, Line rhs) {
         if (System.Object.ReferenceEquals(lhs, rhs)) {
            return true;
         }

         // If one is null, but not both, return false.
         if (((object)lhs == null) || ((object)rhs == null)) {
            return false;
         }

         return lhs.Begin == rhs.Begin
            && lhs.End == rhs.End
            && lhs.Content == rhs.Content;
      }

      public static bool operator !=(Line lhs, Line rhs) {
         return !(lhs == rhs);
      }

      public override bool Equals(object obj) {
         var line = obj as Line;
         return this == line;
      }

      public override int GetHashCode() {
         var hashCode = 1934838938;
         hashCode = hashCode * -1521134295 + Content?.GetHashCode() ?? 387268372;
         hashCode = hashCode * -1521134295 + Begin.GetHashCode();
         hashCode = hashCode * -1521134295 + End.GetHashCode();
         return hashCode;
      }
   }
}
