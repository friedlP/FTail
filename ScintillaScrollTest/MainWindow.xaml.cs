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

      int offset = 0;

      int anchorSelLine;
      int anchorSelCol;
      int curCarLine;
      int curCarCol;

      public MainWindow() {
         InitializeComponent();

         textBox.AssignCmdKey(System.Windows.Forms.Keys.Up, ScintillaNET.Command.LineScrollUp);
         textBox.AssignCmdKey(System.Windows.Forms.Keys.Down, ScintillaNET.Command.LineScrollDown);

         offset = 100;
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
         int diff = textBox.FirstVisibleLine - offset;

         curCarLine = textBox.LineFromPosition(textBox.CurrentPosition);
         curCarCol = textBox.CurrentPosition - textBox.Lines[curCarLine].Position;
         if (textBox.CurrentPosition != textBox.SelectionStart) {
            // forward
            if (textBox.SelectionStart > 0) {
               anchorSelLine = textBox.LineFromPosition(textBox.SelectionStart);
               anchorSelCol = textBox.SelectionStart - textBox.Lines[anchorSelLine].Position;
            }
         } else {
            // backward
            if (textBox.SelectionEnd < textBox.TextLength - 1) {
               anchorSelLine = textBox.LineFromPosition(textBox.SelectionEnd);
               anchorSelCol = textBox.SelectionEnd - textBox.Lines[anchorSelLine].Position;
            }
         }

         anchorSelLine += line - offset;
         curCarLine += line - offset;

         line += diff;

         if (diff != 0) {
            IUpdateTextBox();
         }
      }

      private void IUpdateTextBox() {
         StringBuilder sb = new StringBuilder();
         for (int i = line - offset; i < line + 200; ++i) {
            sb.AppendLine(i + ": " + new string('X', 1000));
         }

         textBox.Document = new ScintillaNET.Document();

         textBox.ReadOnly = false;
         textBox.Text = sb.ToString();
         textBox.ReadOnly = true;

         textBox.FirstVisibleLine = offset;

         Debug.WriteLine($"Begin: {anchorSelLine}, End: {curCarLine}");
         
         int caret = textBox.Lines[curCarLine - line + offset].Position + curCarCol;
         int anchor;
         if (anchorSelLine - line + offset < 0) {
            anchor = 0;
         } else if (anchorSelLine - line + offset > textBox.Lines.Count - 1) {
            anchor = textBox.TextLength;
         } else {
            anchor = textBox.Lines[anchorSelLine - line + offset].Position + anchorSelCol;
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
         if ((e.Change & ScintillaNET.UpdateChange.VScroll) != 0) {
            UpdateText();
         }
      }

      private void textBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e) {
      }
   }

}
