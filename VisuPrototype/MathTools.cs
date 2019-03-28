using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisuPrototype
{
   static class MathTools
   {
      public static long Min(long a, long b) => a < b ? a : b;
      public static long Max(long a, long b) => a > b ? a : b;

      public static double ToRange(double val, double min, double max) => val < min ? min : (val > max ? max : val);
      public static int ToRange(int val, int min, int max) => val < min ? min : (val > max ? max : val);
      public static long ToRange(long val, long min, long max) => val < min ? min : (val > max ? max : val);

      public static long Round(double value) => value > 0 ? (long)(value + 0.5) : -(long)(-value + .5);

      public static long RoundToLongRange(double value)
      {
         if (value > long.MaxValue)
            return long.MaxValue;
         else if (value < long.MinValue)
            return long.MinValue;
         else
         {
            long rValue = Round(value);
            if (value > long.MaxValue / 2 && rValue < 0)
               return long.MaxValue;
            else if (value < long.MinValue / 2 && rValue > 0)
               return long.MinValue;
            else
               return rValue;
         }
      }

      public static long AddInLongRange(long value1, long value2)
      {
         long sum = value1 + value2;
         if (value1 > 0 && value2 > 0)
         {
            if (sum < Max(value1, value2))
               return long.MaxValue;
            else
               return sum;
         }
         else if (value1 < 0 && value2 < 0)
         {
            if (sum > Min(value1, value2))
               return long.MinValue;
            else
               return sum;
         }
         else
         {
            return sum;
         }
      }
   }
}
