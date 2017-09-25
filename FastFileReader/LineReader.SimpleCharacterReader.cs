namespace FastFileReader {
   public partial class LineReader {
      class SimpleCharacterReader : ICharacterReader {
         public bool TryReadCharacter(BlockReader blockReader, long position, out uint character, out long posFirstByte, out long posLastByte) {
            character = 0;
            posFirstByte = -1;
            posLastByte = -1;
            if (position < 0 || position >= blockReader.StreamLength)
               return false;

            character = blockReader.ReadValue(position);
            posFirstByte = position;
            posLastByte = position;
            return true;
         }

         public bool TryReadCharacter(BlockReader blockReader, long position, uint valueAtPos, out uint character, out long posFirstByte, out long posLastByte) {
            character = valueAtPos;
            posFirstByte = position;
            posLastByte = position;
            return true;
         }
      }
   }
}
