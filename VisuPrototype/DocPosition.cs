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

      public static bool operator==(DocPosition lhs, DocPosition rhs)
      {
         if (System.Object.ReferenceEquals(lhs, rhs))
            return true;

         if ((((object)lhs) == null) != (((object)rhs) == null))
            return false;

         return lhs.LineExtent == rhs.LineExtent && lhs.Column == rhs.Column;
      }
      public static bool operator!=(DocPosition lhs, DocPosition rhs)
      {
         return !(lhs == rhs);
      }

      public static bool operator<(DocPosition lhs, DocPosition rhs)
      {
         if (lhs == null)
         {
            if (rhs != null)
               return true;
            else
               return false;
         }
         else if (rhs == null)
            return true;

         if (lhs.LineExtent.Begin < rhs.LineExtent.Begin)
            return true;
         if (lhs.LineExtent.Begin > rhs.LineExtent.Begin)
            return false;
         if (lhs.Column < rhs.Column)
            return true;
         if (lhs.Column > rhs.Column)
            return false;
         if (lhs.LineExtent.End < rhs.LineExtent.End)
            return true;
         if (lhs.LineExtent.End > rhs.LineExtent.End)
            return false;

         return false;
      }
      public static bool operator>(DocPosition lhs, DocPosition rhs)
      {
         return rhs < lhs;
      }

   }
}