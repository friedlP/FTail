using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastFileReader {
   public class FileWatcher {
      string fileName;

      long maxFileLength;
      DateTime encodingValidationTime;

      Ude.CharsetDetector detector;
      Encoding encoding;
      byte[] encodingBuffer;
      int encodingBytesRead;
      bool fileMod;

      public Encoding Encoding => encoding ?? Encoding.Default;

      public FileWatcher(string fileName) {
         this.fileName = Path.GetFullPath(fileName);
         FileSystemWatcher fsw = new FileSystemWatcher(Path.GetDirectoryName(this.fileName), Path.GetFileName(this.fileName));
         fsw.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
         fsw.Changed += Fsw_Changed;
         fsw.Created += Fsw_Created;
         fsw.Deleted += Fsw_Deleted;
         fsw.Renamed += Fsw_Renamed;
         fsw.EnableRaisingEvents = true;

         encodingBuffer = new byte[128 * 1024];
      }

      public Line GetLine(long position) {
         FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);         
         LineReader lineReader = new LineReader(fs, GetEncoding(fs));
         Line l = lineReader.Read(position);
         FeedDetector(l);
         return l;
      }

      public Line NextLine(Line line) {
         if (line == null)
            return null;
         return GetLine(line.End);
      }

      public Line PreviousLine(Line line) {
         if (line == null)
            return null;
         return GetLine(line.Begin - 1);
      }

      private void Reset() {
         maxFileLength = 0;

         detector = null;
         encoding = null;
         encodingBytesRead = 0;

         encodingValidationTime = DateTime.UtcNow;
      }

      private static long Min(long a, long b) => a < b ? a : b;
      private static long Max(long a, long b) => a > b ? a : b;

      private void FeedDetector(Line line) {
         if (detector != null && !detector.IsDone() && line.End > encodingBuffer.Length) {
            detector.Feed(line.Bytes, 0, line.Bytes.Length);
            encoding = EncodingNameConversion(detector.Charset);
         }
      }

      private Encoding GetEncoding(FileStream fs) {
         long streamLength = fs.Length;

         if (streamLength < maxFileLength) {
            Reset();
         }

         byte[] buffer = null;
         int n = 0;
         if (fileMod && DateTime.UtcNow > encodingValidationTime.AddSeconds(1)) {
            buffer = new byte[encodingBuffer.Length];
            fs.Seek(0, SeekOrigin.Begin);
            n = fs.Read(buffer, 0, (int)Min(buffer.Length, streamLength));

            if (n < encodingBytesRead || !AreEqual(encodingBuffer, encodingBytesRead, buffer, encodingBytesRead)) {
               Reset();
            }

            encodingValidationTime = DateTime.UtcNow;
         }

         if (detector == null) {
            detector = new Ude.CharsetDetector();
         }

         if (!detector.IsDone()) {
            if (encodingBytesRead < encodingBuffer.Length) {
               if (n > 0 && n > encodingBytesRead) {
                  detector.Feed(buffer, encodingBytesRead, n - encodingBytesRead);
                  detector.DataEnd();

                  encoding = EncodingNameConversion(detector.Charset);
                  
                  encodingBuffer = buffer;
                  encodingBytesRead = n;
               } else if (encodingBytesRead < encodingBuffer.Length) {
                  fs.Seek(encodingBytesRead, SeekOrigin.Begin);
                  int read = fs.Read(encodingBuffer, encodingBytesRead, (int)Min(encodingBuffer.Length, streamLength) - encodingBytesRead);

                  detector.Feed(encodingBuffer, encodingBytesRead, read);
                  detector.DataEnd();

                  encoding = EncodingNameConversion(detector.Charset);
                  
                  encodingBytesRead += read;
               }
            }
         }
         
         maxFileLength = streamLength;
         return encoding ?? Encoding.Default;
      }

      private bool AreEqual(byte[] array1, int maxBytes1, byte[] array2, int maxBytes2) {
         if (array1 == null && array2 == null)
            return true;

         if ((array1 == null) != (array2 == null))
            return false;

         if (array1.Length < maxBytes1)
            maxBytes1 = array1.Length;

         if (array2.Length < maxBytes2)
            maxBytes2 = array2.Length;

         if (maxBytes1 != maxBytes2)
            return false;
         
         unsafe {
            fixed (byte* a1 = array1, a2 = array2) {
               for (int i = 0; i < maxBytes1; ++i) {
                  if (*(a1 + i) != *(a2 + i)) {
                     return false;
                  }
               }
            }
         }
         return true;
      }

      private static Encoding EncodingNameConversion(string charsetDetectorName) {
         switch (charsetDetectorName) {
            case Ude.Charsets.ASCII:
               return Encoding.GetEncoding("us-ascii");
            case Ude.Charsets.BIG5:
               return Encoding.GetEncoding("big5");
            case Ude.Charsets.EUCJP:
               return Encoding.GetEncoding("euc-jp");
            case Ude.Charsets.EUCKR:
               return Encoding.GetEncoding("euc-kr");
            case Ude.Charsets.EUCTW:
               return null;
            case Ude.Charsets.GB18030:
               return Encoding.GetEncoding("GB18030");
            case Ude.Charsets.HZ_GB_2312:
               return Encoding.GetEncoding("hz-gb-2312");
            case Ude.Charsets.IBM855:
               return Encoding.GetEncoding("IBM855");
            case Ude.Charsets.IBM866:
               return Encoding.GetEncoding("cp866");
            case Ude.Charsets.ISO2022_CN:
               return null;
            case Ude.Charsets.ISO2022_JP:
               return Encoding.GetEncoding("iso-2022-jp");
            case Ude.Charsets.ISO2022_KR:
               return Encoding.GetEncoding("iso-2022-kr");
            case Ude.Charsets.ISO8859_2:
               return Encoding.GetEncoding("iso-8859-2");
            case Ude.Charsets.ISO8859_5:
               return Encoding.GetEncoding("iso-8859-5");
            case Ude.Charsets.ISO8859_8:
               return Encoding.GetEncoding("iso-8859-8");
            case Ude.Charsets.ISO_8859_7:
               return Encoding.GetEncoding("iso-8859-7");
            case Ude.Charsets.KOI8R:
               return Encoding.GetEncoding("koi8-r");
            case Ude.Charsets.MAC_CYRILLIC:
               return Encoding.GetEncoding("x-mac-cyrillic");
            case Ude.Charsets.SHIFT_JIS:
               return Encoding.GetEncoding("shift_jis");
            case Ude.Charsets.TIS620:
               return null;
            case Ude.Charsets.UCS4_2413:
               return null;
            case Ude.Charsets.UCS4_3412:
               return null;
            case Ude.Charsets.UTF16_BE:
               return Encoding.GetEncoding("utf-16BE");
            case Ude.Charsets.UTF16_LE:
               return Encoding.GetEncoding("utf-16");
            case Ude.Charsets.UTF32_BE:
               return Encoding.GetEncoding("utf-32BE");
            case Ude.Charsets.UTF32_LE:
               return Encoding.GetEncoding("utf-32");
            case Ude.Charsets.UTF8:
               return Encoding.GetEncoding("utf-8");
            case Ude.Charsets.WIN1251:
               return Encoding.GetEncoding("windows-1251");
            case Ude.Charsets.WIN1252:
               return Encoding.GetEncoding("windows-1252");
            case Ude.Charsets.WIN1253:
               return Encoding.GetEncoding("windows-1253");
            case Ude.Charsets.WIN1255:
               return Encoding.GetEncoding("windows-1255");
            default:
               return null;
         }
      }

      private void Fsw_Renamed(object sender, RenamedEventArgs e) {
         Reset();
      }

      private void Fsw_Deleted(object sender, FileSystemEventArgs e) {
         Reset();
      }

      private void Fsw_Created(object sender, FileSystemEventArgs e) {
         Reset();
      }

      private void Fsw_Changed(object sender, FileSystemEventArgs e) {
         fileMod = true;
      }
   }
}
