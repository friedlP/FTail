using System.Collections.Generic;

namespace FastFileReader {
   public partial class LineReader {
      class LineEndings {
         public const byte CR = (byte)'\r';
         public const byte LF = (byte)'\n';
         const int carriageReturn = CR;
         const int lineFeed = LF;
         static uint[] stdLineEndings = new uint[] {
            carriageReturn,
            lineFeed
         };
         public virtual IEnumerable<uint> CodePoints => stdLineEndings;
         public virtual bool IsLNewLine(uint val) {
            for (int i = 0; i < stdLineEndings.Length; ++i) {
               if (val == stdLineEndings[i])
                  return true;
            }
            return false;
         }
      }
   }
}
