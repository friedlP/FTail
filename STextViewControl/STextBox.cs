﻿using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Keyboard = System.Windows.Input.Keyboard;

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

   public delegate void ScrollBarParameterChangedHandler(object sender, ScrollBarParameter newScrollBarParameter);
   public delegate void TextChangedHandler(object sender, string newText, int newFirstVisibleLine, SelectionRange selectionRange);
   public delegate void FirstVisibleLineChangedHandler(object sender, int newFirstVisibleLine);
   public delegate void ChangeSelectionHandler(object sender, SelectionRange selectionRange);

   public class SelectionRange
   {
      public SelectionRange(int anchorSelLine, int anchorSelColumn, int curCaretLine, int curCaretColumn)
      {
         AnchorSelLine = anchorSelLine;
         AnchorSelColumn = anchorSelColumn;
         CurCaretLine = curCaretLine;
         CurCaretColumn = curCaretColumn;
      }

      public int AnchorSelLine { get; private set; }
      public int AnchorSelColumn { get; private set; }
      public int CurCaretLine { get; private set; }
      public int CurCaretColumn { get; private set; }
   }

   public interface IScrollLogic
   {
      event ScrollBarParameterChangedHandler VScrollBarParameterChanged;
      event TextChangedHandler TextChanged;
      event FirstVisibleLineChangedHandler FirstVisibleLineChanged;
      event ChangeSelectionHandler ChangeSelection;
      void SetVScroll(double startValue, double endValue);
      void VScroll(long scrollLines);
      void VisibleAreaChanged(int firstVisibleLine, int linesOnScreen);
      void UpdateSelection((int line, int column) curCarPos, (int line, int column) anchorPos, (int line, int column) textEndPos, bool newSelection);
      void ValidateSelection();
      void SelectAll();
      void CopySelection();
   }

   public class STextBox : Scintilla {
      int scrollLines = System.Windows.Forms.SystemInformation.MouseWheelScrollLines;
      bool newSelection = false;
      int lastCaretPos;
      int lastAnchorPos;

      public event ScrollBarValueChangedEventHandler HScrollBarValueChanged;
      public event ScrollBarValueChangedEventHandler VScrollBarValueChanged;
      public ScrollBarParameter HScrollBarValue { get; private set; } = new ScrollBarParameter(0, 1, 0.01, 0.1);
      public ScrollBarParameter VScrollBarValue { get; private set; } = new ScrollBarParameter(0, 1, 0.01, 0.1);

      public STextBox() : base()
      {
         //var contextMenuStrip = new ContextMenuStrip();
         //var selectAll = new ToolStripLabel("Select All");
         //selectAll.Click += SelectAll_Click;
         //contextMenuStrip.Items.Add(selectAll);

         //ContextMenuStrip = contextMenuStrip;

         var contextMenu = new ContextMenu();

         var copy = new MenuItem("Copy");
         copy.Click += Copy_Click;
         contextMenu.MenuItems.Add(copy);

         contextMenu.MenuItems.Add("-");

         var selectAll = new MenuItem("Select All");
         selectAll.Click += SelectAll_Click;
         contextMenu.MenuItems.Add(selectAll);

         ContextMenu = contextMenu;
      }

      private void CopySelection()
      {
         scrollLogic?.CopySelection();
      }

      private void Copy_Click(object sender, EventArgs e)
      {
         CopySelection();
      }

      private void SelectAll_Click(object sender, EventArgs e)
      {
         ISelectAll();
      }

      IScrollLogic scrollLogic;
      public IScrollLogic ScrollLogic {
         get {
            return scrollLogic;
         }
         set {
            if (scrollLogic != null)
            {
               scrollLogic.TextChanged -= OnTextChanged;
               scrollLogic.FirstVisibleLineChanged -= OnFirstVisibleLineChanged;
               scrollLogic.VScrollBarParameterChanged -= OnVScrollBarParameterChanged;
               scrollLogic.ChangeSelection -= OnChangeSelection;
            }
            scrollLogic = value;
            scrollLogic.TextChanged += OnTextChanged;
            scrollLogic.FirstVisibleLineChanged += OnFirstVisibleLineChanged;
            scrollLogic.VScrollBarParameterChanged += OnVScrollBarParameterChanged;
            scrollLogic.ChangeSelection += OnChangeSelection;
         }
      }

      protected void OnChangeSelection(object sender, SelectionRange selectionRange)
      {
         SetSelection(selectionRange);
      }

      protected void OnTextChanged(object sender, string newText, int newFirstVisibleLine, SelectionRange selectionRange)
      {
         base.Document = new ScintillaNET.Document();
         base.ReadOnly = false;
         base.Text = newText;
         base.ReadOnly = true;
         base.FirstVisibleLine = newFirstVisibleLine;
         SetSelection(selectionRange);
         UpdateScrollBars();
      }

      private void SetSelection(SelectionRange selectionRange)
      {
         if (selectionRange == null)
            return;

         int anchorPos = ToPosition(selectionRange.AnchorSelLine, selectionRange.AnchorSelColumn);
         int caretPos = ToPosition(selectionRange.CurCaretLine, selectionRange.CurCaretColumn);

         if (anchorPos != base.AnchorPosition || caretPos != base.CurrentPosition)
         {
            base.AnchorPosition = anchorPos;
            base.CurrentPosition = caretPos;
         }
         lastAnchorPos = anchorPos;
         lastCaretPos = caretPos;
      }

      private int ToPosition(int line, int column)
      {
         if (line < 0)
         {
            return 0;
         }
         else if (line >= base.Lines.Count)
         {
            return base.Lines[base.Lines.Count - 1].EndPosition;
         }
         else
         {
            return base.Lines[line].Position + column;
         }
      }

      protected void OnFirstVisibleLineChanged(object sender, int newFirstVisibleLine)
      {
         base.FirstVisibleLine = newFirstVisibleLine;
      }

      protected void OnVScrollBarParameterChanged(object sender, ScrollBarParameter newScrollBarParameter)
      {
         VScrollBarValue = newScrollBarParameter;
         if (!thumbFixed)
         {
            VScrollBarValueChanged?.Invoke(this, newScrollBarParameter);
            vScrollBarNeedsUpdate = false;
         }
         else
         {
            vScrollBarNeedsUpdate = true;
         }
      }

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
            case Keys.End | Keys.Control:
               VScroll(long.MaxValue);
               return true;
            case Keys.Home | Keys.Control:
               VScroll(long.MinValue);
               return true;
            case Keys.A | Keys.Control:
               ISelectAll();
               return true;
            case Keys.C | Keys.Control:
            case Keys.Insert | Keys.Control:
               CopySelection();
               return true;            
         }

         return base.ProcessCmdKey(ref msg, keyData);
      }

      private void ISelectAll()
      {
         scrollLogic?.SelectAll();
         ScrollLogic?.ValidateSelection();
      }

      public void DataUpdated() {
         ValidateAndUpdateXOffset();
      }

      protected override void OnSizeChanged(EventArgs e) {
         ValidateAndUpdateXOffset();

         ScrollLogic?.VisibleAreaChanged(base.FirstVisibleLine, base.LinesOnScreen);
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
            if (e is HandledMouseEventArgs he)
            {
               he.Handled = true;
            }
         } 
      }

      protected override void OnMouseDown(MouseEventArgs e)
      {
         if (e.Button == MouseButtons.Left)
         {
            newSelection = true;
         }
         base.OnMouseDown(e);
      }

      protected override void OnUpdateUI(UpdateUIEventArgs e) {
         bool validateSelection = false;
         if ((e.Change & UpdateChange.Selection) != 0)
         {
            validateSelection = true;
            UpdateSelection();
         }
         if ((e.Change & UpdateChange.HScroll) != 0) {
            ValidateAndUpdateXOffset();
         }
         if ((e.Change & UpdateChange.VScroll) != 0) {
            ScrollLogic?.VisibleAreaChanged(base.FirstVisibleLine, base.LinesOnScreen);
         }
         if (validateSelection)
         {
            ScrollLogic?.ValidateSelection();
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
      bool thumbFixed;
      bool vScrollBarNeedsUpdate;

      (int caretLine, int caretCol) CaretPos(int pos)
      {
         int caretLine = base.LineFromPosition(pos);
         int caretCol = pos - base.Lines[caretLine].Position;
         return (caretLine, caretCol);
      }

      private void UpdateSelection()
      {
         var curCarPos = CaretPos(base.CurrentPosition);
         var anchorPos = CaretPos(base.AnchorPosition);
         var textEndPos = CaretPos(base.TextLength);
         Debug.WriteLine($"Caret: {lastCaretPos}->{CurrentPosition} Anchor: {lastAnchorPos}->{AnchorPosition}");
         bool shiftPressed = Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) || Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift);
         newSelection &= !shiftPressed || (lastCaretPos != CurrentPosition && lastAnchorPos != AnchorPosition);
         scrollLogic?.UpdateSelection(curCarPos, anchorPos, textEndPos, newSelection);
         newSelection = false;
      }

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

      public void SetThumbFixed(bool thumbFixed) {
         this.thumbFixed = thumbFixed;
         if (!thumbFixed && vScrollBarNeedsUpdate)
         {
            VScrollBarValueChanged?.Invoke(this, VScrollBarValue);
            vScrollBarNeedsUpdate = false;
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

         //Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] HScroll: min={min}, max={max}, smallChange={smallChange}, largeChange={largeChange}");
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
            
      public void SetVScroll(double startValue, double endValue) {
         ScrollLogic?.SetVScroll(startValue, endValue);
      }
      
      private void VScroll(long scrollLines) {
         if (!thumbFixed)
         {
            ScrollLogic?.VScroll(scrollLines);
         }
      }

      private static double ToRange(double val, double min, double max) => val < min ? min : (val > max ? max : val);

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

      /// <summary>
      /// Disables the invisible but still existing horizontal scrollbar of the Scintilla control element.
      /// </summary>
      private void UpdateScrollBars() {
         ShowScrollBar(this.Handle, SB_HORZ, false);
      }

      [DllImport("user32.dll")]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, [MarshalAs(UnmanagedType.Bool)] bool bShow);
   }
   
}

