using FastFileReader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STextViewControl;
using static VisuPrototype.MathTools;

namespace VisuPrototype
{
   class ScrollLogic : IScrollLogic
   {
      public event ScrollBarParameterChangedHandler VScrollBarParameterChanged;
      public event FirstVisibleLineChangedHandler FirstVisibleLineChanged;
      public event TextChangedHandler TextChanged;
      public event ChangeSelectionHandler ChangeSelection;

      ScrollBarParameter curScrollBarParameter = new ScrollBarParameter(0, 1, 0.01, 0.1);
      double? scrollPosition;

      int textViewFirstVisibleLine;
      int textViewLinesOnScreen;
      string textViewText;

      readonly FileWatcher fw;
      readonly LineBuffer lb;
      LineRange lineRange;
      
      int bufferLines;

      public ScrollLogic(FileWatcher fileWatcher)
      {
         textViewFirstVisibleLine = 0;
         textViewLinesOnScreen = 30;
         fw = fileWatcher;
         lb = new LineBuffer(fw);
         UpdateRequiredBufferedLines();
      }

      public void Init(int position, Origin origin, int linesOnScreen)
      {
         textViewLinesOnScreen = linesOnScreen;
         UpdateRequiredBufferedLines();

         MonitorChanges(() =>
         {
            UpdateRange(position, origin);
         });
      }
      
      public void SetVScroll(double startValue, double endValue)
      {
         MonitorChanges(() =>
         {
            scrollPosition = ScrollPos(startValue, endValue);
            IntSetVScroll(startValue, endValue);
            scrollPosition = null;
         });
      }


      public void VScroll(long scrollLines)
      {
         MonitorChanges(() =>
         {
            IntVScroll(scrollLines * curScrollBarParameter.SmallChange, scrollLines);
         });
      }

      public void VisibleAreaChanged(int firstVisibleLine, int linesOnScreen)
      {
         int scroll = firstVisibleLine - textViewFirstVisibleLine;
         bool linesOnScreenChanged = textViewLinesOnScreen != linesOnScreen;

         textViewFirstVisibleLine = firstVisibleLine;
         textViewLinesOnScreen = linesOnScreen;
         if (linesOnScreenChanged)
            UpdateRequiredBufferedLines();

         MonitorChanges(() =>
         {
            if (scroll != 0)
            {
               IntVScroll(scroll * curScrollBarParameter.SmallChange, scroll);
            }
            else if (linesOnScreenChanged)
            {
               IntVScroll(force: true);
            }
         });         
      }

      private void MonitorChanges(Action action)
      {
         var oldScrollBarParameter = curScrollBarParameter;
         var oldText = textViewText;
         var oldFirstVisibleLine = textViewFirstVisibleLine;
         action();
         if (textViewText != oldText)
         {
            SelectionRange selectionRange = GetSelectionRange();
            selectionDirty = false;

            TextChanged?.Invoke(this, textViewText, textViewFirstVisibleLine, selectionRange);
         }
         else if (textViewFirstVisibleLine != oldFirstVisibleLine)
         {
            FirstVisibleLineChanged?.Invoke(this, textViewFirstVisibleLine);
         }

         if(ScrollBarParameterChanged(oldScrollBarParameter, curScrollBarParameter))
         {
            VScrollBarParameterChanged?.Invoke(this, curScrollBarParameter);
         }
      }

      private SelectionRange GetSelectionRange()
      {
         DocPosition anchor = anchorPosition;
         if (curCaretPos < endSelPos)
         {
            if (anchor < endSelPos)
               anchor = endSelPos;
         }
         else if (curCaretPos > startSelPos)
         {
            if (anchor > startSelPos)
               anchor = startSelPos;
         }

         SelectionRange selectionRange = null;
         if (curCaretPos != null && anchor != null)
            selectionRange = new SelectionRange(LineFromExtent(anchor.LineExtent), anchor.Column, LineFromExtent(curCaretPos.LineExtent), curCaretPos.Column);
         return selectionRange;
      }

      public void ValidateSelection()
      {
         if (selectionDirty)
         {
            SelectionRange selectionRange = GetSelectionRange();
            selectionDirty = false;
            ChangeSelection?.Invoke(this, selectionRange);
         }
      }

      private static bool ScrollBarParameterChanged(ScrollBarParameter scrollBarParameter1, ScrollBarParameter scrollBarParameter2)
      {
         return scrollBarParameter1.StartValue != scrollBarParameter2.StartValue
            || scrollBarParameter1.EndValue != scrollBarParameter2.EndValue
            || scrollBarParameter1.SmallChange != scrollBarParameter2.SmallChange
            || scrollBarParameter1.LargeChange != scrollBarParameter2.LargeChange;
      }

      private void IntSetVScroll(double startValue, double endValue)
      {
         (double diff, long diffLines) = GetChange(curScrollBarParameter.StartValue, curScrollBarParameter.EndValue, startValue, endValue);

         if (diffLines != 0)
            IntVScroll(diff, diffLines);
      }

      private void IntVScroll(double scroll = 0.0, long scrollLines = 0, bool force = false)
      {
         VScrollHandler(scroll, scrollLines, force);
         UpdateVScrollBar();
      }

      private int FullyVisibleLines => textViewLinesOnScreen;
      private int PartiallyVisibleLines => FullyVisibleLines + 1;

      private void VScrollHandler(double scroll, long scrollLines, bool force)
      {
         if (lineRange != null && lineRange.RequestedLine != null)
         {
            long linesOnScreen = FullyVisibleLines;

            long maxScrollNeg = lineRange.PreviousExtents.Count == 0 ? -lineRange.PreviousLines.Count : long.MinValue;
            long maxScrollPos = lineRange.NextExtents.Count == 0 ? lineRange.NextLines.Count : long.MaxValue;
            long possibleScroll = ToRange(scrollLines, maxScrollNeg, maxScrollPos);
            long possibleScrollLastLine = ToRange(AddInLongRange(possibleScroll, linesOnScreen), maxScrollNeg, maxScrollPos);
            if (AddInLongRange(possibleScrollLastLine, -possibleScroll) < linesOnScreen)
            {
               // Last line can't be scrolled as far as first line (end of file reached)
               possibleScroll = ToRange(AddInLongRange(possibleScrollLastLine, -linesOnScreen + 1), maxScrollNeg, possibleScrollLastLine);
            }

            if (possibleScroll != 0 || force)
            {
               UpdateRange(scroll, possibleScroll);
            }
         }
      }

      private void UpdateRange(double scroll, long scrollLinesPossible)
      {
         Extent newExtent = lineRange.RelExtent(scrollLinesPossible);
         if (newExtent != null)
         {
            UpdateRange(newExtent.Begin, Origin.Begin);
         }
         else
         {
            int linesOnScreen = FullyVisibleLines;
            double lineLen = (double)(lineRange.LastExtent.End - lineRange.FirstExtent.Begin) / lineRange.ExtentsCount;
            double vis = linesOnScreen * lineLen;
            double streamLenMod = lineRange.StreamLength - vis;

            long pos;
            Origin origin;
            double sValue = scrollPosition ?? 0.5; // 0.5: No correction
            double cFactor = 1 - sValue * (1 - sValue) * 4;
            if (sValue <= 0.5)
            {
               origin = Origin.Begin;
               pos = ToRange(RoundToLongRange(LineAt(0).Begin + streamLenMod * scroll), 0, lineRange.StreamLength - 1);
               long p = RoundToLongRange(cFactor * (sValue * streamLenMod - pos));
               pos = ToRange(pos + p, 0, lineRange.StreamLength - 1);   // Position at the begin of the line
               pos += Round(sValue * lineLen);                          // Position within the line
            }
            else
            {
               origin = Origin.End;
               pos = ToRange(RoundToLongRange(LineAt(linesOnScreen - 1).End + streamLenMod * scroll), 0, lineRange.StreamLength);
               long p = RoundToLongRange(cFactor * ((lineRange.StreamLength - (1 - sValue) * streamLenMod) - pos));
               pos = ToRange(pos + p, 0, lineRange.StreamLength);       // Position at the end of the line 
               pos -= lineRange.StreamLength + 1;                       // --> From the end of the stream
               pos -= Round((1 - sValue) * lineLen);                    // Position within the line
            }

            UpdateRange(pos, origin);
         }
      }

      private Line LineAt(int line)
      {
         int prevLineCount = lineRange.PreviousLines.Count;
         int nextLineCount = lineRange.NextLines.Count;
         if (line > 0 && nextLineCount > 0)
         {
            return lineRange.NextLines[ToRange(line - 1, 0, nextLineCount - 1)];
         }
         else if (line < 0 && prevLineCount > 0)
         {
            return lineRange.PreviousLines[prevLineCount - 1 - ToRange(-line - 1, 0, prevLineCount - 1)];
         }
         else
         {
            return lineRange.RequestedLine;
         }
      }

      private void UpdateRange(long position, Origin origin)
      {
         int nextLines = bufferLines + (origin == Origin.Begin ? textViewLinesOnScreen : 0);
         int prevLines = bufferLines + (origin == Origin.End ? textViewLinesOnScreen : 0);
         LineRange newLineRange = lb.ReadRange(position, origin, prevLines, nextLines, 1000, 1000);
         if (origin == Origin.End)
            newLineRange = ShiftLineRange(newLineRange, -(textViewLinesOnScreen - 1));
         RangeUpdated(newLineRange);
      }

      private LineRange ShiftLineRange(LineRange lineRange, int shift)
      {
         int prevLineCount = lineRange.PreviousLines.Count;
         int nextLineCount = lineRange.NextLines.Count;
         if (shift < 0 && prevLineCount > 0)
         {
            shift = Min(-shift, prevLineCount);
            int newPrevLineCount = prevLineCount - shift;
            List<Line> prev = lineRange.PreviousLines.Take(newPrevLineCount).ToList();
            Line cur = lineRange.PreviousLines.Skip(newPrevLineCount).First();
            List<Line> next = lineRange.PreviousLines.Skip(newPrevLineCount + 1).Append(lineRange.RequestedLine).Concat(lineRange.NextLines).ToList();
            return new LineRange(cur, prev, next, lineRange.PreviousExtents, lineRange.NextExtents, lineRange.StreamLength);
         }
         else if (shift > 0 && nextLineCount > 0)
         {
            shift = Min(shift, nextLineCount);
            List<Line> prev = lineRange.PreviousLines.Append(lineRange.RequestedLine).Concat(lineRange.NextLines.Take(shift - 1)).ToList();
            Line cur = lineRange.NextLines.Skip(shift - 1).First();
            List<Line> next = lineRange.NextLines.Skip(shift).ToList();
            return new LineRange(cur, prev, next, lineRange.PreviousExtents, lineRange.NextExtents, lineRange.StreamLength);
         }
         else
         {
            return lineRange;
         }
      }

      private void UpdateVScrollBar()
      {
         int linesOnScreen = FullyVisibleLines;

         (double min, double max) = GetVisibleRange(linesOnScreen);
         (double smallChange, double largeChange) = CalcScrollChange(min, max, linesOnScreen, 1);

         var newScrollBarParameter = new ScrollBarParameter(min, max, smallChange, largeChange);
         UpdateScrollBarParameter(newScrollBarParameter);
      }

      private void UpdateScrollBarParameter(ScrollBarParameter parameter)
      {
         bool update = curScrollBarParameter.StartValue != parameter.StartValue
            || curScrollBarParameter.EndValue != parameter.EndValue
            || curScrollBarParameter.SmallChange != parameter.SmallChange
            || curScrollBarParameter.LargeChange != parameter.LargeChange;

         if (update)
         {
            curScrollBarParameter = parameter;
            IntSetVScroll(curScrollBarParameter.StartValue, curScrollBarParameter.EndValue);
         }
            
      }

      private (double smallChange, double largeChange) CalcScrollChange(double min, double max, double visibleSize, double scrollOne)
      {
         min = ToRange(min, 0, 1);
         max = ToRange(max, 0, 1);
         if (min >= max || visibleSize <= 0)
            return (0, 0);

         double visiblePart = max - min;
         if (visiblePart == 1)
            return (1, 1);

         double remainingScrollArea = 1 - visiblePart;
         double totalSize = visibleSize / visiblePart;
         double q = totalSize * remainingScrollArea;
         double smallChange = ToRange(scrollOne / q, 0, 1);
         double largeChange = ToRange(visibleSize / q, 0, 1);
         return (smallChange, largeChange);
      }

      private (double min, double max) GetVisibleRange(int linesOnScreen)
      {
         (double min, double max) = VisibleRangeCalculator(linesOnScreen);
         return (ToRange(min, 0, 1), ToRange(max, 0, 1));
      }

      private (double min, double max) VisibleRangeCalculator(int linesOnScreen)
      {
         if (lineRange == null || lineRange.RequestedLine == null)
            return (0, 1);

         long nPrev = lineRange.PreviousExtents.Count + lineRange.PreviousLines.Count;
         long nNext = lineRange.NextExtents.Count + lineRange.NextLines.Count;
         long n = nPrev + 1 + nNext;

         double relPosFirstInLoaded = (double)nPrev / n;
         double relPosLastInLoaded = (double)(nPrev + linesOnScreen) / n;

         double relPosFirstLoaded = (double)lineRange.FirstExtent.Begin / lineRange.StreamLength;
         double relPosLastLoaded = (double)lineRange.LastExtent.End / lineRange.StreamLength;

         double loadedPart = relPosLastLoaded - relPosFirstLoaded;

         double relBeginPos = relPosFirstLoaded + loadedPart * relPosFirstInLoaded;
         double relEndPos = relPosFirstLoaded + loadedPart * relPosLastInLoaded;

         if (relEndPos > 1)
         {
            relBeginPos = ToRange(relBeginPos - (relEndPos - 1), 0, 1);
            relEndPos = 1;
         }
         return (relBeginPos, relEndPos);
      }

      static double ScrollPos(double startValue, double endValue)
      {
         double v = 1 - (endValue - startValue);
         return v > 0 ? ToRange(startValue / v, 0, 1) : 0;
      }

      private (double diff, long diffLines) GetChange(double oldStartValue, double oldEndValue, double newStartValue, double newEndValue)
      {
         double scrollPositionPrev = ScrollPos(oldStartValue, oldEndValue);
         double scrollPosition = ScrollPos(newStartValue, newEndValue);

         double lineChange = curScrollBarParameter.SmallChange;
         double diff = scrollPosition - scrollPositionPrev;

         double dLines = diff / lineChange;
         long diffLines = RoundToLongRange(dLines);

         //Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{nameof(ScrollLogic)}.{nameof(GetChange)}] Change: {scrollPositionPrev} -> {scrollPosition}, diff={diff}, lines={diffLines}");

         return (diff, diffLines);
      }

      private void RangeUpdated(LineRange range)
      {
         lineRange = range;
         SetText(lineRange);
      }

      private void SetText(LineRange range)
      {
         int maxLines = textViewLinesOnScreen;
         StringBuilder stringBuilder = new StringBuilder();
         int firstVisibleLine = 0;

         if (range != null)
         {
            int prev = (int)Min(50, range.PreviousLines.Count);
            int next = (int)Min(50 + maxLines, range.NextLines.Count);
            firstVisibleLine = prev;
            List<FastFileReader.Line> lines = new List<FastFileReader.Line>();
            for (int i = 0; i < prev; ++i)
            {
               lines.Add(range.PreviousLines[range.PreviousLines.Count - prev + i]);
            }
            if (range.RequestedLine != null) lines.Add(range.RequestedLine);
            for (int i = 0; i < next; ++i)
            {
               lines.Add(range.NextLines[i]);
            }

            lines.ForEach(l => stringBuilder.AppendLine(l.Content.TrimEnd()));
         }
         string text = stringBuilder.ToString().TrimEnd(new char[] { '\r', '\n' });
         if (textViewText != text || textViewFirstVisibleLine != firstVisibleLine)
         {
            //Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{nameof(ScrollLogic)}.{nameof(SetText)}] Update Text: {lineRange.RequestedLine.Content}");
            textViewText = text;
            textViewFirstVisibleLine = firstVisibleLine;
            DataUpdated();
         }
      }

      private void DataUpdated()
      {
         //ValidateAndUpdateXOffset();
         IntVScroll(0, 0);
      }

      private void UpdateRequiredBufferedLines()
      {
         bufferLines = ToRange(PartiallyVisibleLines * 2, 200, 1000);
      }

      DocPosition curCaretPos;
      DocPosition startSelPos;
      DocPosition endSelPos;
      DocPosition anchorPosition;
      bool selectionDirty;

      public void UpdateSelection(
         (int line, int column) curCarPos,
         (int line, int column) anchorPos,
         (int line, int column) textEndPos,
         bool newSelection)
      {
         (int line, int column) textStartPos = (0, 0);

         selectionDirty = true;

         DocPosition newCaretPosition = CreateDocPosition(curCarPos);
         DocPosition newAnchorPosition = CreateDocPosition(anchorPos);

         Debug.WriteLine($"curCarPos=({curCarPos}) anchorPos={anchorPos})");

         if (curCarPos == anchorPos && newCaretPosition == startSelPos && startSelPos == endSelPos || newSelection)
         {
            curCaretPos = newCaretPosition;
            anchorPosition = newAnchorPosition;
            if (newCaretPosition >= newAnchorPosition)
            {
               startSelPos = newAnchorPosition;
               endSelPos = curCaretPos;
               Debug.WriteLine($"New (forward): curCaretPos=({curCaretPos?.LineExtent.Begin}/{curCaretPos?.Column}) "
                  + $"anchorPosition=({anchorPosition?.LineExtent.Begin}/{anchorPosition?.Column}) "
                  + $"startSelPos=({startSelPos?.LineExtent.Begin}/{startSelPos?.Column}) "
                  + $"endSelPos=({endSelPos?.LineExtent.Begin}/{endSelPos?.Column})");
            }
            else
            {
               startSelPos = curCaretPos;
               endSelPos = newAnchorPosition;
               Debug.WriteLine($"New (backward): curCaretPos=({curCaretPos?.LineExtent.Begin}/{curCaretPos?.Column}) "
                  + $"anchorPosition=({anchorPosition?.LineExtent.Begin}/{anchorPosition?.Column}) "
                  + $"startSelPos=({startSelPos?.LineExtent.Begin}/{startSelPos?.Column}) "
                  + $"endSelPos=({endSelPos?.LineExtent.Begin}/{endSelPos?.Column})");
            }
         }
         else if (newCaretPosition > curCaretPos && curCarPos != textStartPos)
         {
            curCaretPos = newCaretPosition;
            Debug.WriteLine($"Upd (forward): curCaretPos=({curCaretPos?.LineExtent.Begin}/{curCaretPos?.Column}) "
               + $"anchorPosition=({anchorPosition?.LineExtent.Begin}/{anchorPosition?.Column}) "
               + $"startSelPos=({startSelPos?.LineExtent.Begin}/{startSelPos?.Column}) "
               + $"endSelPos=({endSelPos?.LineExtent.Begin}/{endSelPos?.Column})");
         }
         else if (newCaretPosition < curCaretPos && curCarPos != textEndPos)
         {
            curCaretPos = newCaretPosition;
            Debug.WriteLine($"Upd (backward): curCaretPos=({curCaretPos?.LineExtent.Begin}/{curCaretPos?.Column}) "
               + $"anchorPosition=({anchorPosition?.LineExtent.Begin}/{anchorPosition?.Column}) "
               + $"startSelPos=({startSelPos?.LineExtent.Begin}/{startSelPos?.Column}) "
               + $"endSelPos=({endSelPos?.LineExtent.Begin}/{endSelPos?.Column})");
         }
      }

      private DocPosition CreateDocPosition((int line, int column) curCarPos)
      {
         return new DocPosition(LineExtent(curCarPos.line), curCarPos.column);
      }

      private Extent LineExtent(int line)
      {
         return lineRange.RelExtent(line - textViewFirstVisibleLine);
      }

      private int LineFromExtent(Extent extent)
      {
         if (lineRange.FirstLine.Extent.Begin > extent.Begin)
            return int.MinValue;
         else if (lineRange.LastLine.Extent.Begin < extent.Begin)
            return int.MaxValue;
         else
         {
            int linePos = 0;
            foreach(var line in lineRange.AllLines)
            {
               if (line.Begin == extent.Begin)
                  return linePos - lineRange.PreviousLines.Count + textViewFirstVisibleLine;
               ++linePos;
            }
         }
         return 0;
      }
   }

}
