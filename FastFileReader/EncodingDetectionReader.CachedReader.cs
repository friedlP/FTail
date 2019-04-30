using System;
using System.Collections.Generic;
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

         private Line Find(List<Line> lines, long position)
         {
            int pos = Find(lines, (l) => l.Extent, position);
            if (pos >= 0)
               return lines[pos];
            else
               return null;
         }

         private Extent Find(List<Extent> extents, long position)
         {
            int pos = Find(extents, (e) => e, position);
            if (pos >= 0)
               return extents[pos];
            else
               return null;
         }

         private int Find<T>(List<T> elements, Func<T, Extent> toExtent, long position)
         {
            if (elements == null || elements.Count == 0 || position < toExtent(elements[0]).Begin || position >= toExtent(elements[elements.Count - 1]).End)
               return -1;
            int min = 0;
            int max = elements.Count - 1;
            while (min <= max)
            {
               int pos = min + (max - min) / 2;
               Extent e = toExtent(elements[pos]);
               if (e.Begin <= pos && e.End > pos)
                  return pos;
               else if (e.Begin > pos)
                  max = pos - 1;
               else
                  min = pos + 1;
            }
            return -1;
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
                  line = Find(cache.PreviousLines, position);
               }
               else if (cache.NextLines.Count > 0 && cache.NextLines[0].Begin <= position && cache.NextLines[cache.NextLines.Count - 1].End > position)
               {
                  line = Find(cache.PreviousLines, position);
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
                  extent = Find(cache.PreviousExtents, position);
               }
               else if (cache.NextExtents.Count > 0 && cache.NextExtents[0].Begin <= position && cache.NextExtents[cache.NextExtents.Count - 1].End > position)
               {
                  extent = Find(cache.NextExtents, position);
               }
            }
            if (extent != null && extent.End < cache.StreamLength)
               return extent;

            return lineReader.GetLineExtent(position);
         }
      }
   }
}
