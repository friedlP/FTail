using System;
using System.Linq;

namespace FastFileReader
{
   public abstract partial class EncodingDetectionReader
   {
      class CachedReader
      {
         LineReader lineReader;
         LineRange cache;
         Action<RawLine> feedDetector;

         public CachedReader(LineReader lineReader, LineRange cache, Action<RawLine> feedDetector)
         {
            this.lineReader = lineReader;
            this.cache = cache;
            this.feedDetector = feedDetector;
         }

         public Line Read(long position)
         {
            Line line = null;
            if (cache?.RequestedLine != null)
            {
               if (cache.RequestedLine.Begin <= position && cache.RequestedLine.End > position)
               {
                  line = cache.RequestedLine;
               }
               else if (cache.PreviousLines.Count > 0 && cache.PreviousLines[0].Begin <= position && cache.PreviousLines[cache.PreviousLines.Count - 1].End > position)
               {
                  foreach (Line l in cache.PreviousLines)
                  {
                     if (l.Begin <= position && l.End > position)
                     {
                        line = l;
                        break;
                     }
                  }
               }
               else if (cache.NextLines.Count > 0 && cache.NextLines[0].Begin <= position && cache.NextLines[cache.NextLines.Count - 1].End > position)
               {
                  foreach (Line l in cache.NextLines)
                  {
                     if (l.Begin <= position && l.End > position)
                     {
                        line = l;
                        break;
                     }
                  }
               }
            }
            if (line != null && line.End < cache.StreamLength)
               return line;

            RawLine rawLine = lineReader.Read(position);
            feedDetector?.Invoke(rawLine);
            return rawLine;
         }

         public Line ReadNext(Line line)
         {
            return Read(line.End);
         }

         public Line ReadPrevious(Line line)
         {
            return Read(line.Begin - 1);
         }

         public Extent GetLineExtent(long position)
         {
            Extent extent = null;
            if (cache?.RequestedLine != null)
            {
               if (cache.FirstLine.Begin <= position && cache.LastLine.End > position)
               {
                  extent = Read(position).Extent;
               }
               else if (cache.PreviousExtents.Count > 0 && cache.PreviousExtents[0].Begin <= position && cache.PreviousExtents[cache.PreviousExtents.Count - 1].End > position)
               {
                  foreach (Extent e in cache.PreviousExtents)
                  {
                     if (e.Begin <= position && e.End > position)
                     {
                        extent = e;
                        break;
                     }
                  }
               }
               else if (cache.NextExtents.Count > 0 && cache.NextExtents[0].Begin <= position && cache.NextExtents[cache.NextExtents.Count - 1].End > position)
               {
                  foreach (Extent e in cache.NextExtents)
                  {
                     if (e.Begin <= position && e.End > position)
                     {
                        extent = e;
                        break;
                     }
                  }
               }
            }
            if (extent != null && extent.End < cache.StreamLength)
               return extent;

            return lineReader.GetLineExtent(position);
         }
      }
   }
}
