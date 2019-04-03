using FastFileReader;
using System.Collections.Generic;

namespace VisuPrototype
{

   class DocPosition
   {
      public DocPosition(Extent lineExtent, int column)
      {
         LineExtent = lineExtent;
         Column = column;
      }

      public Extent LineExtent { get; private set; }
      public int Column { get; private set; }

      public override bool Equals(object obj)
      {
         return obj is DocPosition position &&
                EqualityComparer<Extent>.Default.Equals(LineExtent, position.LineExtent) &&
                Column == position.Column;
      }

      public override int GetHashCode()
      {
         var hashCode = 311296326;
         hashCode = hashCode * -1521134295 + EqualityComparer<Extent>.Default.GetHashCode(LineExtent);
         hashCode = hashCode * -1521134295 + Column.GetHashCode();
         return hashCode;
      }

      public static bool operator ==(DocPosition lhs, DocPosition rhs)
      {
         if (System.Object.ReferenceEquals(lhs, rhs))
            return true;

         if ((((object)lhs) == null) != (((object)rhs) == null))
            return false;

         return lhs.LineExtent == rhs.LineExtent && lhs.Column == rhs.Column;
      }
      public static bool operator !=(DocPosition lhs, DocPosition rhs)
      {
         return !(lhs == rhs);
      }

      public static bool operator <(DocPosition lhs, DocPosition rhs)
      {
         return Compare(lhs, rhs) == ComparisionResult.RightBigger;
      }
      public static bool operator >(DocPosition lhs, DocPosition rhs)
      {
         return Compare(lhs, rhs) == ComparisionResult.LeftBigger;
      }
      public static bool operator <=(DocPosition lhs, DocPosition rhs)
      {
         return Compare(lhs, rhs) != ComparisionResult.LeftBigger;
      }
      public static bool operator >=(DocPosition lhs, DocPosition rhs)
      {
         return Compare(lhs, rhs) != ComparisionResult.RightBigger;
      }

      private enum ComparisionResult
      {
         LeftBigger,
         Equal,
         RightBigger
      }

      private static ComparisionResult Compare(DocPosition lhs, DocPosition rhs)
      {
         if (lhs == null)
         {
            if (rhs != null)
               return ComparisionResult.RightBigger;
            else
               return ComparisionResult.Equal;
         }
         else if (rhs == null)
            return ComparisionResult.LeftBigger;

         if (lhs.LineExtent.Begin < rhs.LineExtent.Begin)
            return ComparisionResult.RightBigger;
         if (lhs.LineExtent.Begin > rhs.LineExtent.Begin)
            return ComparisionResult.LeftBigger;
         if (lhs.Column < rhs.Column)
            return ComparisionResult.RightBigger;
         if (lhs.Column > rhs.Column)
            return ComparisionResult.LeftBigger;
         if (lhs.LineExtent.End < rhs.LineExtent.End)
            return ComparisionResult.RightBigger;
         if (lhs.LineExtent.End > rhs.LineExtent.End)
            return ComparisionResult.LeftBigger;

         return ComparisionResult.Equal;
      }
   }
}