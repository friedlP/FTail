using System;

namespace FastFileReader {
   public partial class LineReader {
      class UTF32CharacterReader : ICharacterReader {
         public bool TryReadCharacter(BlockReader blockReader, long position, out uint character, out long posFirstByte, out long posLastByte) {
            if (!(blockReader is BlockReader32))
               throw new ArgumentException($"Type of '{nameof(blockReader)}' should be '{typeof(BlockReader32).Name}'");

            character = 0;
            posFirstByte = -1;
            posLastByte = -1;

            position = blockReader.PositionFirstByte(position);

            if (position < 0 || position + 3 >= blockReader.StreamLength)
               return false;

            character = blockReader.ReadValue(position);
            posFirstByte = position;
            posLastByte = position + 3;
            return true;
         }

         public bool TryReadCharacter(BlockReader blockReader, long position, uint valueAtPos, out uint character, out long posFirstByte, out long posLastByte) {
            if (!(blockReader is BlockReader32))
               throw new ArgumentException($"Type of '{nameof(blockReader)}' should be '{typeof(BlockReader32).Name}'");

            position = blockReader.PositionFirstByte(position);
            character = valueAtPos;
            posFirstByte = position;
            posLastByte = position + 3;
            return true;
         }
      }
   }
}
