using System.Collections.Generic;
using System.Linq;

namespace FastFileReader {
   public partial class LineReader {
      class UnicodeLineEndings : LineEndings {
         const int formFeed = '\u000C';
         const int newLine = '\u0085';
         const int lineSeparator = '\u2028';
         const int paragraphSeparator = '\u2029';
         static uint[] utfLineEndings = new uint[] {
            formFeed,
            newLine,
            lineSeparator,
            paragraphSeparator
         };
         public override IEnumerable<uint> CodePoints => base.CodePoints.Concat(utfLineEndings);
         public override bool IsLNewLine(uint val) {
            if (base.IsLNewLine(val))
               return true;
            for (int i = 0; i < utfLineEndings.Length; ++i) {
               if (val == utfLineEndings[i])
                  return true;
            }
            return false;
         }
      }
   }
}
