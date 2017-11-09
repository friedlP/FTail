using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ScintillaScrollTest {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window {
      int line = 0;
      int collumn = 0;

      int nOffset = 0;
      int pOffset = 0;

      int anchorSelLine;
      int anchorSelCol;
      int curCarLine;
      int curCarCol;

      public MainWindow() {
         InitializeComponent();

         nOffset = 100;
         pOffset = 200;
         IUpdateTextBox();
      }

      private void WindowsFormsHost_Initialized(object sender, EventArgs e) {
         UpdateText();
      }

      private void UpdateText() {
         if (Dispatcher.CheckAccess()) {
            IUpdateText();
         } else {
            Dispatcher.BeginInvoke(new Action(() => {
               IUpdateText();
            }), DispatcherPriority.ContextIdle, null);
         }
      }


      private void IUpdateText() {
         int diff = textBox.FirstVisibleLine - nOffset;

         //UpdateSelcetion();

         line += diff;

         if (diff != 0) {
            IUpdateTextBox();
         }
      }

      private void UpdateSelcetion() {
         curCarLine = textBox.LineFromPosition(textBox.CurrentPosition);
         curCarCol = textBox.CurrentPosition - textBox.Lines[curCarLine].Position;
         curCarLine += line - nOffset;

         if (textBox.CurrentPosition == textBox.SelectionStart && textBox.CurrentPosition == textBox.SelectionEnd) {
            anchorSelLine = curCarLine;
            anchorSelCol = curCarCol;
         } else if (textBox.CurrentPosition != textBox.SelectionStart) {
            // forward
            int newAnchorSelLine = textBox.LineFromPosition(textBox.SelectionStart);
            int newAnchorSelCol = textBox.SelectionStart - textBox.Lines[newAnchorSelLine].Position;
            newAnchorSelLine += line - nOffset;
            if (textBox.SelectionStart > 0 || newAnchorSelLine < anchorSelLine
                  || newAnchorSelLine == anchorSelLine && newAnchorSelCol < anchorSelCol) {
               anchorSelLine = newAnchorSelLine;
               anchorSelCol = newAnchorSelCol;
            }
         } else {
            // backward
            int newAnchorSelLine = textBox.LineFromPosition(textBox.SelectionEnd);
            int newAnchorSelCol = textBox.SelectionEnd - textBox.Lines[newAnchorSelLine].Position;
            newAnchorSelLine += line - nOffset;
            if (textBox.SelectionEnd < textBox.TextLength || newAnchorSelLine > anchorSelLine
                  || newAnchorSelLine == anchorSelLine && newAnchorSelCol > anchorSelCol) {
               anchorSelLine = newAnchorSelLine;
               anchorSelCol = newAnchorSelCol;
            }
         }
      }

      private void IUpdateTextBox() {
         StringBuilder sb = new StringBuilder();

         if (line >= -200) {
            nOffset = 100;
         } else if (line >= -300) {
            nOffset = 300 + line;
         } else {
            nOffset = 0;
         }
         
         if (line <= 200) {
            pOffset = 200;
         } else if (line <= 400) {
            pOffset = 400 - line;
         } else {
            pOffset = 0;
         }

         Debug.WriteLine($"pOffset: {pOffset}, min={line - nOffset}, max={line + pOffset - 1}");

         for (int i = line - nOffset; i < line + pOffset; ++i) {
            sb.AppendLine(i + ": " + new string('X', 1000));
         }

         textBox.Document = new ScintillaNET.Document();

         textBox.ReadOnly = false;
         textBox.Text = sb.ToString();
         textBox.ReadOnly = true;

         textBox.FirstVisibleLine = nOffset;

         Debug.WriteLine($"Begin: {anchorSelLine}, End: {curCarLine}");

         int caret;
         if (curCarLine - line + nOffset < 0) {
            caret = 0;
         } else if (curCarLine - line + nOffset > textBox.Lines.Count - 1) {
            caret = textBox.TextLength;
         } else {
            caret = textBox.Lines[curCarLine - line + nOffset].Position + curCarCol;
         }

         int anchor;
         if (anchorSelLine - line + nOffset < 0) {
            anchor = 0;
         } else if (anchorSelLine - line + nOffset > textBox.Lines.Count - 1) {
            anchor = textBox.TextLength;
         } else {
            anchor = textBox.Lines[anchorSelLine - line + nOffset].Position + anchorSelCol;
         }

         textBox.SetSelection(caret, anchor);

         Debug.WriteLine(line);
      }

      private void TextBox_StyleNeeded(object sender, ScintillaNET.StyleNeededEventArgs e) {
         ScintillaNET.Scintilla scintilla = ((ScintillaNET.Scintilla)sender);

         var startPos = scintilla.GetEndStyled();
         var endPos = e.Position;

         scintilla.StartStyling(100);
         scintilla.SetStyling(100, 1);
         scintilla.SetStyling(100, 2);
      }

      private void textBox_UpdateUI(object sender, ScintillaNET.UpdateUIEventArgs e) {
         Debug.WriteLine(e.Change.ToString());

         if ((e.Change & ScintillaNET.UpdateChange.Selection) != 0) {
            UpdateSelcetion();
         }
        
         if ((e.Change & ScintillaNET.UpdateChange.VScroll) != 0) {
            UpdateText();
         }
      }

      private void textBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e) {
      }
   }

}
