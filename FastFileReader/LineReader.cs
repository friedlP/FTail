using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System;

namespace FastFileReader {
   public partial class LineReader {
      public const char BOM = '\uFEFF';

      BlockReader blockReader;
      Encoding encoding;
      LineEndings lineEndings;
      ICharacterReader charReader;
      ISearchData searchData;

      public LineReader(Stream stream, Encoding encoding) {
         this.encoding = encoding;
         switch (encoding.WebName) {
            case "utf-8":
               blockReader = new BlockReader(stream);
               lineEndings = new UnicodeLineEndings();
               charReader = new UTF8CharacterReader();
               break;
            case "utf-16":
               blockReader = new BlockReader16(stream, false);
               lineEndings = new UnicodeLineEndings();
               charReader = new UTF16CharacterReader();
               break;
            case "utf-16BE":
               blockReader = new BlockReader16(stream, true);
               lineEndings = new UnicodeLineEndings();
               charReader = new UTF16CharacterReader();
               break;
            case "utf-32":
               blockReader = new BlockReader32(stream, false);
               lineEndings = new UnicodeLineEndings();
               charReader = new UTF32CharacterReader();
               break;
            case "utf-32BE":
               blockReader = new BlockReader32(stream, true);
               lineEndings = new UnicodeLineEndings();
               charReader = new UTF32CharacterReader();
               break;
            default:
               blockReader = new BlockReader(stream);
               lineEndings = new LineEndings();
               charReader = new SimpleCharacterReader();
               break;
         }

         InitNewLineMarker();

         TrimCharacters = lineEndings.CodePoints
            .Where(c => c <= char.MaxValue && !char.IsSurrogate((char)c))
            .Select(c => (char)c)
            .Concat(new char[] { BOM })
            .ToArray();
      }

      public long StreamLength => blockReader.StreamLength;

      public char[] TrimCharacters { get; private set; }
      
      public RawLine ReadNext(Line line) {
         if (line == null)
            return null;

         return Read(line.End);
      }

      public RawLine ReadPrevious(Line line) {
         if (line == null)
            return null;

         return Read(line.Begin - 1);
      }

      void InitNewLineMarker() {
         int codePointSize = blockReader.MinCodePointSize;
         List<uint> lMarkers = new List<uint>();
         foreach (uint cPoint in lineEndings.CodePoints) {
            byte[] bytes = encoding.GetBytes(new char[] { (char)cPoint });
            uint m = 0;
            for (int i = bytes.Length - codePointSize; i < bytes.Length; ++i) {
               m = (m << 8) | bytes[i];
            }
            lMarkers.Add(m);
         }
         searchData = blockReader.CreateSearchData(lMarkers);
      }

      public RawLine Read(long position) {
         int codePointSize = blockReader.MinCodePointSize;
         position = blockReader.PositionFirstByte(position);

         if (position < 0 || position >= blockReader.StreamLength)
            return null;

         long nlBegin;
         long nlEnd;

         // Search begin of line
         long lineBegin = position;
         if (IsNewLine(blockReader, lineEndings, charReader, lineBegin, out nlBegin, out nlEnd)) {
            lineBegin = nlBegin - codePointSize;
         }
         while (lineBegin >= 0) {
            lineBegin = blockReader.FindBackward(lineBegin, searchData);
            if (lineBegin < 0)
               break;
            if (IsNewLine(blockReader, lineEndings, charReader, lineBegin, out nlBegin, out nlEnd)) {
               lineBegin = nlEnd;
               break;
            }
            lineBegin -= codePointSize;
         }
         if (lineBegin < 0)
            lineBegin = 0;
                  
         // Search end of line
         long lineEnd = position;
         while (lineEnd < blockReader.StreamLength) {
            lineEnd = blockReader.FindForward(lineEnd, searchData);
            if (lineEnd >= 0) {
               if (IsNewLine(blockReader, lineEndings, charReader, lineEnd, out nlBegin, out nlEnd)) {
                  lineEnd = nlEnd;
                  break;
               }
            } else {
               lineEnd = blockReader.StreamLength;
               break;
            }
            lineEnd += codePointSize;
         }

         // Read line
         byte[] strBytes = blockReader.ReadRange(lineBegin, lineEnd);

         string str = encoding.GetString(strBytes);
         return new RawLine(str, lineBegin, lineEnd, strBytes);
      }

      bool IsNewLine(BlockReader blockReader, LineEndings lineEndings, ICharacterReader charReader, long position, out long begin, out long end) {
         begin = -1;
         end = -1;

         uint v = blockReader.ReadValue(position);
         if (v == LineEndings.CR) {
            begin = position;
            position += blockReader.MinCodePointSize;
            end = position;
            if (position < blockReader.StreamLength) {
               v = blockReader.ReadValue(position);
               if (v == LineEndings.LF) {
                  end += blockReader.MinCodePointSize;
               }
            }
            return true;
         } else if (v == LineEndings.LF) {
            begin = position;
            end = position + blockReader.MinCodePointSize;
            position -= blockReader.MinCodePointSize;
            if (position >= 0) {
               v = blockReader.ReadValue(position);
               if (v == LineEndings.CR) {
                  begin = position;
               }
            }
            return true;
         } else {
            if (charReader.TryReadCharacter(blockReader, position, v, out uint ch, out long first, out long last)) {
               if (lineEndings.IsLNewLine(ch)) {
                  begin = blockReader.PositionFirstByte(first);
                  end = blockReader.PositionFirstByte(last) + blockReader.MinCodePointSize;
                  return true;
               }
            }
         }
         return false;
      }

      interface ICharacterReader {
         bool TryReadCharacter(BlockReader blockReader, long position, out uint character, out long posFirstByte, out long posLastByte);
         bool TryReadCharacter(BlockReader blockReader, long position, uint valueAtPos, out uint character, out long posFirstByte, out long posLastByte);
      }
   }
}
