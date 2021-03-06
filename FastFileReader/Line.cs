﻿namespace FastFileReader
{
   public class Line
   {
      public string Content { get; private set; }
      public Extent Extent { get; private set; }

      public long Begin => Extent.Begin;
      public long End => Extent.End;

      public Line(string content, Extent extent)
      {
         this.Content = content;
         this.Extent = extent;
      }

      public static bool operator ==(Line lhs, Line rhs)
      {
         if (System.Object.ReferenceEquals(lhs, rhs))
         {
            return true;
         }

         // If one is null, but not both, return false.
         if (((object)lhs == null) || ((object)rhs == null))
         {
            return false;
         }

         return lhs.Extent == rhs.Extent
            && lhs.Content == rhs.Content;
      }

      public static bool operator !=(Line lhs, Line rhs)
      {
         return !(lhs == rhs);
      }

      public override bool Equals(object obj)
      {
         var line = obj as Line;
         return this == line;
      }

      public override int GetHashCode()
      {
         var hashCode = 1934838938;
         hashCode = hashCode * -1521134295 + Content?.GetHashCode() ?? 387268372;
         hashCode = hashCode * -1521134295 + Extent?.GetHashCode() ?? 387268372;
         return hashCode;
      }
   }
}
