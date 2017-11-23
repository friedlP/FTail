using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace STextViewControl {
   public delegate void ScrollBarValueChangedEventHandler(object sender, ScrollBarParameter parameter);
   public delegate (double min, double max) VisibleRangeCalculator(int lines, int firstVisibleLine, int linesOnScreen);
   public delegate void VScrollHandler(STextBox sTextBox, double scroll, long scrollLines);

   public class ScrollBarParameter {
      public double StartValue { get; private set; }
      public double EndValue { get; private set; }
      public double SmallChange { get; private set; }
      public double LargeChange { get; private set; }

      public ScrollBarParameter(double startValue, double endValue, double smallChange, double largeChange) {
         StartValue = startValue;
         EndValue = endValue;
         SmallChange = smallChange;
         LargeChange = largeChange;
      }
   }

   public class STextBox : Scintilla {
      int scrollLines = System.Windows.Forms.SystemInformation.MouseWheelScrollLines;

      public event ScrollBarValueChangedEventHandler HScrollBarValueChanged;
      public event ScrollBarValueChangedEventHandler VScrollBarValueChanged;
      public ScrollBarParameter HScrollBarValue { get; private set; } = new ScrollBarParameter(0, 1, 0.01, 0.1);
      public ScrollBarParameter VScrollBarValue { get; private set; } = new ScrollBarParameter(0, 1, 0.01, 0.1);
      VisibleRangeCalculator vRangeCalc = VisibleRange;
      VScrollHandler vScrollHandler = HandleVScroll;
            
      protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
         switch (keyData) {
            case Keys.Left:
               ScrollLeft(HScrollPixels);
               return true;
            case Keys.Right:
               ScrollRight(HScrollPixels);
               return true;
            case Keys.End:
               ScrollRight(base.ScrollWidth);
               return true;
            case Keys.Home:
               ScrollLeft(base.ScrollWidth);
               return true;
            case Keys.Up:
               VScroll(-1);
               return true;
            case Keys.Down:
               VScroll(1);
               return true;
            case Keys.PageUp:
               VScroll(-base.LinesOnScreen);
               return true;
            case Keys.PageDown:
               VScroll(base.LinesOnScreen);
               return true;
         }

         return base.ProcessCmdKey(ref msg, keyData);
      }
      
      protected override void OnSizeChanged(EventArgs e) {
         ValidateAndUpdateXOffset();
         VScroll(0, 0);
         base.OnSizeChanged(e);
      }

      protected override void OnMouseWheel(MouseEventArgs e) {
         if (e.Delta != 0) {
            bool shiftPressed = (Control.ModifierKeys & Keys.Shift) != Keys.None;
            int sign = e.Delta > 0 ? -1 : 1;
            if (shiftPressed) {
               HScroll(sign * HScrollPixels * scrollLines);
            } else {
               VScroll(sign * scrollLines);
            }
            var he = e as HandledMouseEventArgs;
            if (he != null) he.Handled = true;
         } 
      }

      protected override void OnUpdateUI(UpdateUIEventArgs e) {
         if ((e.Change & UpdateChange.HScroll) != 0) {
            ValidateAndUpdateXOffset();
         }
         if ((e.Change & UpdateChange.VScroll) != 0) {
            UpdateVScrollBar();
         }
         base.OnUpdateUI(e);
      }

      int TextAreaWidth {
         get {
            int left = base.XOffset + base.PointXFromPosition(base.Lines[base.LineFromPosition(base.TextLength)].Position);
            return base.ClientSize.Width - left - 2;
         }
      }

      bool insideValidateAndUpdateXOffset;
      private void ValidateAndUpdateXOffset() {
         if (insideValidateAndUpdateXOffset)
            return;

         try {
            insideValidateAndUpdateXOffset = true;

            IValidateAndUpdateXOffset();
         } finally {
            insideValidateAndUpdateXOffset = false;
         }
      }

      private void IValidateAndUpdateXOffset() {
         UpdateScrollBars();

         int xOff = base.XOffset;
         int textAreaWidth = TextAreaWidth;
         int maxOffset = base.ScrollWidth - textAreaWidth;

         if (maxOffset < 0) {
            maxOffset = 0;
         }
         if (xOff > maxOffset) {
            base.XOffset = maxOffset;
         }

         UpdateHScrollBar();
      }

      private void UpdateHScrollBar() {
         double scrollwidth = base.ScrollWidth;
         double xOffset = base.XOffset;
         double hScrollPix = HScrollPixels;
         double textAreaWidth = TextAreaWidth;

         (double min, double max) = (0, 1);
         
         if (scrollwidth > 0) {
            min = ToRange(xOffset / scrollwidth, 0, 1);
            max = ToRange((xOffset + textAreaWidth) / scrollwidth, 0, 1);
         }

         (double smallChange, double largeChange) = CalcScrollChange(min, max, textAreaWidth, hScrollPix);

         Debug.WriteLine($"HScroll: min={min}, max={max}, smallChange={smallChange}, largeChange={largeChange}");
         HScrollBarValue = new ScrollBarParameter(min, max, smallChange, largeChange);
         HScrollBarValueChanged?.Invoke(this, HScrollBarValue);
      }

      public void SetHScroll(double startValue, double endValue) {
         double v = 1 - (endValue - startValue);
         double scrollPosition = ToRange(v > 0 ? startValue / v : 0, 0, 1);

         int textAreaWidth = TextAreaWidth;
         int maxOffset = base.ScrollWidth - textAreaWidth;

         int xOff;
         if (maxOffset < 0) {
            xOff = 0;
         } else {
            xOff = (int)(maxOffset * scrollPosition + .5);
         }
         if (xOff != base.XOffset) {
            base.XOffset = xOff;
         }
      }

      public void SetVisibleRangeCalculator(VisibleRangeCalculator vRangeCalculator) {
         this.vRangeCalc = vRangeCalculator ?? VisibleRange;
      }

      private (double min, double max) GetVisibleRange(int lines, int firstVisibleLine, int linesOnScreen) {
         (double min, double max) = vRangeCalc(lines, firstVisibleLine, linesOnScreen);
         return (ToRange(min, 0, 1), ToRange(max, 0, 1));
      }

      private static (double min, double max) VisibleRange(int lines, int firstVisibleLine, int linesOnScreen) {
         double min = 0;
         double max = 1;
         if (lines > 0) {
            min = ToRange((double)firstVisibleLine / lines, 0, 1);
            max = ToRange((double)(firstVisibleLine + linesOnScreen) / lines, 0, 1);
         }
         return (min, max);
      }

      public void SetVerticalScrollHandler(VScrollHandler vScrollHandler) {
         this.vScrollHandler = vScrollHandler ?? HandleVScroll;
      }

      private static void HandleVScroll(STextBox sTextBox, double scroll, long scrollLines) {
         sTextBox.LineScroll((int)scrollLines, 0);
      }

      private void UpdateVScrollBar() {
         int lines = base.Lines.Count;
         int firstVisLine = base.FirstVisibleLine;
         int linesOnScreen = base.LinesOnScreen;

         (double min, double max) = GetVisibleRange(lines, firstVisLine, linesOnScreen);
         (double smallChange, double largeChange) = CalcScrollChange(min, max, linesOnScreen, 1);

         Debug.WriteLine($"VScroll: min={min}, max={max}, smallChange={smallChange}, largeChange={largeChange}");
         VScrollBarValue = new ScrollBarParameter(min, max, smallChange, largeChange);
         VScrollBarValueChanged?.Invoke(this, VScrollBarValue);
      }
      
      public void SetVScroll(double startValue, double endValue) {
         double vPrev = 1 - (VScrollBarValue.EndValue - VScrollBarValue.StartValue);
         double scrollPositionPrev = vPrev > 0 ? VScrollBarValue.StartValue / vPrev : 0;

         double v = 1 - (endValue - startValue);
         double scrollPosition = v > 0 ? startValue / v : 0;

         double lineChange = VScrollBarValue.SmallChange;
         double diff = scrollPosition - scrollPositionPrev;

         double dLines = diff / lineChange;
         int diffLines = dLines < 0 ? -(int)(-dLines + .5) : (int)(dLines + .5);

         if (diffLines != 0)
            VScroll(diff, diffLines);
      }

      private void VScroll(long scrollLines) {
         VScroll(scrollLines * VScrollBarValue.SmallChange, scrollLines);
      }

      private void VScroll(double scroll, long scrollLines) {
         Debug.WriteLine($"VScroll: scroll={scroll}, scrollLines={scrollLines}");
         vScrollHandler(this, scroll, scrollLines);
         UpdateVScrollBar();
      }

      private static double ToRange(double val, double min, double max) => val < min ? min : (val > max ? max : val);

      private (double smallChange, double largeChange) CalcScrollChange(double min, double max, double visibleSize, double scrollOne) {
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

      private int HScrollPixels => base.TextWidth(0, "O");

      private void HScroll(int pixels) {
         if (pixels >= 0) {
            ScrollRight(pixels);
         } else {
            ScrollLeft(-pixels);
         }
      }

      private void ScrollRight(int pixels) {
         int xOff = base.XOffset;
         int width = base.ScrollWidth - base.ClientSize.Width;

         if (xOff + pixels > width) {
            int left = xOff + base.PointXFromPosition(base.Lines[base.LineFromPosition(base.TextLength)].Position);
            width += left + 2;
         }

         int w = xOff + pixels;
         base.XOffset = w <= width ? w : width;
      }

      private void ScrollLeft(int pixels) {
         int w = base.XOffset - pixels;
         base.XOffset = w >= 0 ? w : 0;
      }

      const int SB_HORZ = 0;
      const int SB_VERT = 1;
      const int SB_CTL = 2;
      const int SB_BOTH = 3;

      private void UpdateScrollBars() {
         ShowScrollBar(this.Handle, SB_HORZ, false);
      }

      [DllImport("user32.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, [MarshalAs(UnmanagedType.Bool)] bool bShow);
   }
   
}

