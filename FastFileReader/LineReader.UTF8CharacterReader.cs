namespace FastFileReader
{
   public partial class LineReader
   {
      class UTF8CharacterReader : ICharacterReader
      {
         // 0xxxxxxx
         // 110xxxxx 10xxxxxx
         // 1110xxxx 10xxxxxx 10xxxxxx
         // 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx

         public bool TryReadCharacter(BlockReader blockReader, long position, out uint character, out long posFirstByte, out long posLastByte)
         {
            character = 0;
            posFirstByte = -1;
            posLastByte = -1;
            if (position < 0 || position >= blockReader.StreamLength)
               return false;

            uint val = blockReader.ReadValue(position);
            if (val < 0b1000_0000)
            {
               character = val;
               posFirstByte = position;
               posLastByte = position;
               return true;
            }

            return TryReadOther(blockReader, position, val, out character, out posFirstByte, out posLastByte);
         }

         public bool TryReadCharacter(BlockReader blockReader, long position, uint valueAtPos, out uint character, out long posFirstByte, out long posLastByte)
         {
            if (valueAtPos < 0b1000_0000)
            {
               character = valueAtPos;
               posFirstByte = position;
               posLastByte = position;
               return true;
            }
            else
            {
               character = 0;
               posFirstByte = -1;
               posLastByte = -1;
               return TryReadOther(blockReader, position, valueAtPos, out character, out posFirstByte, out posLastByte);
            }
         }

         private bool TryReadOther(BlockReader blockReader, long position, uint valueAtPos, out uint character, out long posFirstByte, out long posLastByte)
         {
            character = 0;
            posFirstByte = -1;
            posLastByte = -1;
            uint bVal = valueAtPos;

            uint val = 0;
            int n = 0;
            while (bVal >= 0b1000_0000 && bVal < 0b1100_0000 && n < 4)
            {
               val = ((bVal & 0b0011_1111) << (n * 6)) | val;
               ++n;
               --position;
               if (position < 0)
                  return false;

               bVal = blockReader.ReadValue(position);
            }
            long fb = position;
            long lb = position;

            if (bVal >= 0b1111_0000 && bVal < 0b1111_1000)
            {
               if (n > 3) return false;
               val = ((bVal & 0b0000_0111) << (n * 6)) | val;
               position += n + 1;
               n = 3 - n;
               lb += 3;
            }
            else if (bVal >= 0b1110_0000 && bVal < 0b1111_0000)
            {
               if (n > 2) return false;
               val = ((bVal & 0b0000_1111) << (n * 6)) | val;
               position += n + 1;
               n = 2 - n;
               lb += 2;
            }
            else if (bVal >= 0b1100_0000 && bVal < 0b1110_0000)
            {
               if (n > 1) return false;
               val = ((bVal & 0b0001_1111) << (n * 6)) | val;
               position += n + 1;
               n = 1 - n;
               lb += 1;
            }
            else
            {
               return false;
            }

            while (n > 0)
            {
               if (position >= blockReader.StreamLength)
                  return false;
               bVal = blockReader.ReadValue(position);
               if (bVal < 0b1000_0000 || bVal >= 0b1100_0000)
                  return false;
               val = ((val << 6)) | (bVal & 0b0011_1111);
               --n;
            }

            character = val;
            posFirstByte = fb;
            posLastByte = lb;
            return true;
         }
      }
   }
}
