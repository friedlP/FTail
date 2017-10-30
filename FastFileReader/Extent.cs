namespace FastFileReader {
   public class Extent {
      public long Begin { get; private set; }
      public long End { get; private set; }

      public Extent(long begin, long end) {
         Begin = begin;
         End = end;
      }

      public static bool operator ==(Extent lhs, Extent rhs) {
         if (System.Object.ReferenceEquals(lhs, rhs)) {
            return true;
         }

         // If one is null, but not both, return false.
         if (((object)lhs == null) || ((object)rhs == null)) {
            return false;
         }

         return lhs.Begin == rhs.Begin
            && lhs.End == rhs.End;
      }

      public static bool operator !=(Extent lhs, Extent rhs) {
         return !(lhs == rhs);
      }

      public override bool Equals(object obj) {
         var extent = obj as Extent;
         return this == extent;
      }

      public override int GetHashCode() {
         var hashCode = 1903003160;
         hashCode = hashCode * -1521134295 + Begin.GetHashCode();
         hashCode = hashCode * -1521134295 + End.GetHashCode();
         return hashCode;
      }
   }
}
