using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FastFileReader {

   public enum Origin {
      Begin,
      End
   }

   public delegate void ErrorEventHandler(object sender, Exception e);
   public delegate void EncodingChangedEventHandler(object sender, Encoding enc);
   public delegate void StreamChangedEventHandler(object sender);
   public delegate void StreamUnavailableHandler(object sender);

   public abstract class EncodingDetectionReader {
      public event ErrorEventHandler Error;
      public event EncodingChangedEventHandler EcondingChanged;
      public event StreamChangedEventHandler StreamChanged;
      public event StreamUnavailableHandler StreamUnavailable;

      long maxStreamLength;
      
      Ude.CharsetDetector detector;
      Encoding encoding;
      byte[] encodingBuffer;
      int encodingBytesRead;
      bool lineAtBufferEndCompleted;

      public EncodingDetectionReader() {
         encodingBuffer = new byte[128 * 1024];
      }

      protected abstract Stream GetStream();
      protected abstract void CloseStream(Stream stream);

      public Encoding Encoding => encoding ?? Encoding.Default;

      public LineRange ReadRange(long position, Origin origin, int maxPrev, int maxNext) {
         Stream stream = null;
         try {
            int cp = Encoding.CodePage;

            stream = GetStream();
            if (stream == null) {
               Reset();
               StreamUnavailable?.Invoke(this);
               return new LineRange();
            }

            LineReader lineReader = new LineReader(stream, GetEncoding(stream));
            if (origin == Origin.End) {
               position = stream.Length + position;
               if (position < 0) position = 0;
            }

            RawLine curLine = null;
            List<RawLine> prev = new List<RawLine>();
            List<RawLine> next = new List<RawLine>();

            curLine = ReadRange(lineReader, position, maxPrev, maxNext, prev, next);

            int enc = Encoding.CodePage;

            foreach (RawLine line in prev) {
               FeedDetector(line, lineReader);
            }
            if (curLine != null) FeedDetector(curLine, lineReader);
            foreach (RawLine line in next) {
               FeedDetector(line, lineReader);
            }

            if (Encoding.CodePage != enc) {
               // Read line again with new encoding
               lineReader = new LineReader(stream, Encoding);
               curLine = ReadRange(lineReader, position, maxPrev, maxNext, prev, next);
            }

            if (Encoding.CodePage != cp)
               EcondingChanged?.Invoke(this, Encoding);

            return new LineRange(curLine, prev.ConvertAll<Line>(l => (Line)l), next.ConvertAll<Line>(l => (Line)l), lineReader.StreamLength);
         } catch (Exception e) {
            try {
               CloseStream(stream);
            } catch {
            }
            HandleError(e);
            return new LineRange();
         }
      }

      private static RawLine ReadRange(LineReader lineReader, long position, int maxPrev, int maxNext, List<RawLine> prev, List<RawLine> next) {
         prev.Clear();
         next.Clear();
         RawLine curLine = lineReader.Read(position);

         if (curLine != null) {
            RawLine l = curLine;
            for (int prevRead = 0; prevRead < maxPrev; ++prevRead) {
               l = lineReader.ReadPrevious(l);
               if (l != null) {
                  prev.Insert(0, l);
               } else {
                  break;
               }
            }

            l = curLine;
            for (int nextRead = 0; nextRead < maxNext; ++nextRead) {
               l = lineReader.ReadNext(l);
               if (l != null) {
                  next.Add(l);
               } else {
                  break;
               }
            }
         }

         return curLine;
      }

      public Line GetLine(long position) {
         LineRange range = ReadRange(position, Origin.Begin, 0, 0);
         return range.RequestedLine;
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

      protected virtual void Reset() {
         maxStreamLength = 0;

         detector = null;
         encoding = null;
         encodingBytesRead = 0;
         lineAtBufferEndCompleted = false;
      }

      protected void HandleError(Exception e) {
         Reset();
         Error?.Invoke(this, e);
      }

      protected void HandleStreamChanged() {
         StreamChanged?.Invoke(this);
      }

      private static long Min(long a, long b) => a < b ? a : b;
      private static long Max(long a, long b) => a > b ? a : b;

      private void FeedDetector(RawLine line, LineReader lineReader) {
         if (detector != null && !detector.IsDone() && line.End >= encodingBuffer.Length) {

            // The encoding buffer could have a part of a character as last byte --> Read the
            // rest of this line
            if (!lineAtBufferEndCompleted) {
               RawLine bufferEndLine = lineReader.Read(encodingBuffer.Length - 1);
               if (bufferEndLine.End > encodingBuffer.Length) {
                  FeedDetector(bufferEndLine.Bytes, (int)(encodingBuffer.Length - bufferEndLine.Begin), (int)(bufferEndLine.End - encodingBuffer.Length));
               }
               if (bufferEndLine.End < lineReader.StreamLength) {
                  lineAtBufferEndCompleted = true;
               }
            }

            if (line.Begin >= encodingBuffer.Length) {
               FeedDetector(line.Bytes, 0, line.Bytes.Length);
            }
            detector.DataEnd();
            encoding = EncodingNameConversion(detector.Charset);
         }
      }

      private Encoding GetEncoding(Stream stream) {
         long streamLength = stream.Length;

         if (streamLength < maxStreamLength) {
            Reset();
         }

         byte[] buffer = null;
         int n = 0;
         if (EncodingValidationRequired()) {
            buffer = new byte[encodingBuffer.Length];
            stream.Seek(0, SeekOrigin.Begin);
            n = stream.Read(buffer, 0, (int)Min(buffer.Length, streamLength));

            if (n < encodingBytesRead || !AreEqual(encodingBuffer, encodingBytesRead, buffer, encodingBytesRead)) {
               Reset();
            }

            EncodingValidated();
         }

         if (detector == null) {
            detector = new Ude.CharsetDetector();
         }

         if (!detector.IsDone()) {
            if (encodingBytesRead < encodingBuffer.Length) {
               if (n > 0 && n > encodingBytesRead) {
                  FeedDetector(buffer, encodingBytesRead, n - encodingBytesRead);
                  detector.DataEnd();

                  encoding = EncodingNameConversion(detector.Charset);

                  encodingBuffer = buffer;
                  encodingBytesRead = n;
               } else if (encodingBytesRead < encodingBuffer.Length) {
                  int toRead = (int)Min(encodingBuffer.Length, streamLength) - encodingBytesRead;
                  if (toRead > 0) {
                     stream.Seek(encodingBytesRead, SeekOrigin.Begin);
                     int read = stream.Read(encodingBuffer, encodingBytesRead, toRead);
                     if (read > 0) {
                        FeedDetector(encodingBuffer, encodingBytesRead, read);
                        detector.DataEnd();

                        encoding = EncodingNameConversion(detector.Charset);

                        encodingBytesRead += read;
                     }
                  }
               }
            }
         }

         maxStreamLength = streamLength;
         return encoding ?? Encoding.Default;
      }

      protected virtual void EncodingValidated() {
      }

      protected virtual bool EncodingValidationRequired() {
         return false;
      }

      private void FeedDetector(byte[] buffer, int offset, int len) {
         byte[] b = new byte[len];
         Array.Copy(buffer, offset, b, 0, len);
         detector.Feed(b, 0, len);
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
   }
}
