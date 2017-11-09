using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScintillaScrollTest {
    class STextBox : Scintilla {
      protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
         switch (keyData) {
            case Keys.Left:
               ScrollLeft(HScrollPixels);
               //base.LineScroll(0, -1);
               return true;
            case Keys.Right:
               ScrollRight(HScrollPixels);
               //base.LineScroll(0, 1);
               return true;
            case Keys.End:
               ScrollRight(base.ScrollWidth);
               return true;
            case Keys.Home:
               ScrollLeft(base.ScrollWidth);
               return true;
            case Keys.Up:
               base.LineScroll(-1, 0);
               return true;
            case Keys.Down:
               base.LineScroll(1, 0);
               return true;
            case Keys.PageUp:
               base.LineScroll(-base.LinesOnScreen, 0);
               return true;
            case Keys.PageDown:
               base.LineScroll(base.LinesOnScreen, 0);
               return true;
         }

         return base.ProcessCmdKey(ref msg, keyData);
      }

      protected override void OnUpdateUI(UpdateUIEventArgs e) {
         if ((e.Change & UpdateChange.HScroll) != 0) {
            int xOff = base.XOffset;
            int width = base.ScrollWidth - base.ClientSize.Width;

            if (xOff > width) {
               int left = xOff + base.PointXFromPosition(base.Lines[base.LineFromPosition(base.TextLength)].Position);
               width += left + 2;
            }

            if (xOff  > width) {
               base.XOffset = width;
            }
         }
         base.OnUpdateUI(e);
      }

      private int HScrollPixels => base.TextWidth(0, "O");

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
   }
}
