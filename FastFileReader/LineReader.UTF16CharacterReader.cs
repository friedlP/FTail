using System;

namespace FastFileReader {
   public partial class LineReader {
      class UTF16CharacterReader : ICharacterReader {

         // Reserved:       0b1101_1000_0000_0000 - 0b1101_1111_1111_1111

         // High surrogate: 0b1101_1000_0000_0000 - 0b1101_1011_1111_1111
         // Low surrogate:  0b1101_1100_0000_0000 - 0b1101_1111_1111_1111

         public bool TryReadCharacter(BlockReader blockReader, long position, out uint character, out long posFirstByte, out long posLastByte) {
            if (!(blockReader is BlockReader16))
               throw new ArgumentException($"Type of '{nameof(blockReader)}' should be '{typeof(BlockReader16).Name}'");

            character = 0;
            posFirstByte = -1;
            posLastByte = -1;

            //position -= position & 0b1;
            position = blockReader.PositionFirstByte(position);

            if (position < 0 || position + 1 >= blockReader.StreamLength)
               return false;

            uint val = blockReader.ReadValue(position);

            if (val < 0b1101_1000_0000_0000 || val >= 0b1110_0000_0000_0000) {
               character = val;
               posFirstByte = position;
               posLastByte = position + 1;
               return true;
            } else {
               return TryReadOther(blockReader, position, val, out character, out posFirstByte, out posLastByte);
            }
         }

         public bool TryReadCharacter(BlockReader blockReader, long position, uint valueAtPos, out uint character, out long posFirstByte, out long posLastByte) {
            if (!(blockReader is BlockReader16))
               throw new ArgumentException($"Type of '{nameof(blockReader)}' should be '{typeof(BlockReader16).Name}'");

            position = blockReader.PositionFirstByte(position);
            if (valueAtPos < 0b1101_1000_0000_0000 || valueAtPos >= 0b1110_0000_0000_0000) {
               character = valueAtPos;
               posFirstByte = position;
               posLastByte = position + 1;
               return true;
            } else {
               return TryReadOther(blockReader, position, valueAtPos, out character, out posFirstByte, out posLastByte);
            }
         }

         private bool TryReadOther(BlockReader blockReader, long position, uint valueAtPos, out uint character, out long posFirstByte, out long posLastByte) {
            character = 0;
            posFirstByte = -1;
            posLastByte = -1;
            uint val = valueAtPos;

            if (val >= 0b1101_1100_0000_0000 && val < 0b1110_0000_0000_0000) {
               // Low surrogate found
               if (position + 3 >= blockReader.StreamLength)
                  return false;
               val = val & 0b0000_0011_1111_1111;
               val = (val | ((blockReader.ReadValue(position + 2) & 0b0000_0011_1111_111) << 10)) + 0x10000;
               character = val;
               posFirstByte = position;
               posLastByte = position + 3;
            } else if (val >= 0b1101_1100_0000_0000 && val < 0b1110_0000_0000_0000) {
               // High surrogate found
               if (position - 2 < 0)
                  return false;
               val = (val & 0b0000_0011_1111_1111) << 10;
               val = (val | (blockReader.ReadValue(position - 2) & 0b0000_0011_1111_111)) + 0x10000;
               character = val;
               posFirstByte = position - 2;
               posLastByte = position + 1;
            }
            return false;
         }
      }
   }
}