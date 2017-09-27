using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace WpfApp1 {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window {
      long i = 0;
      Stopwatch stopwatch = new Stopwatch();
      long t0;
      //bool initialized;

      public MainWindow() {
         InitializeComponent();
         stopwatch.Start();
         t0 = 0;
      }

      //private void WindowsFormsHost_ChildChanged(object sender, System.Windows.Forms.Integration.ChildChangedEventArgs e) {
      //   if (!initialized) {
      //      Dispatch();
      //      initialized = true;
      //   }
      //}

      void Dispatch() {
         Task.Run(() => {
            //Thread.Sleep(100);
            Dispatcher.BeginInvoke(new Action(() => {
               long ticks = stopwatch.ElapsedTicks;
               StringBuilder sb = new StringBuilder();
               sb.AppendLine(i + ": " + (1000.0 * (ticks - t0) / Stopwatch.Frequency) + " ms");
               if (i > 0 && ticks > 0) {
                  sb.AppendLine(i + ": " + (1000.0 * ((double)ticks / i) / Stopwatch.Frequency) + " ms");
                  sb.AppendLine(i + ": " + (1.0 / (((double)ticks / i) / Stopwatch.Frequency)) + " Hz");
               }
               for (int j = 0; j < 200; ++j) {
                  //sb.AppendLine(j + ": " + new string((char)('A' + ((i + j) % 26)), 1000));
                  sb.AppendLine(j + ": " + new string('X', 1000));
               }
               //textBox.ReleaseDocument(textBox.Document);
               textBox.Document = new ScintillaNET.Document();
               var color = System.Drawing.Color.FromArgb((int)(i / 256 % 256), (int)(i / 16 % 256), (int)(i % 256));
               textBox.Styles[1].ForeColor = color;
               textBox.Styles[1].Bold = true;
               //textBox.Styles[1].Font = "Vivaldi";
               textBox.Styles[2].ForeColor = System.Drawing.Color.FromArgb(255 - color.R, 255 - color.G, 255 - color.B);
               textBox.Styles[2].Italic = true;
               textBox.Text = sb.ToString();
               ++i;
               t0 = ticks;
               Dispatch();
            }), DispatcherPriority.ContextIdle, null);
         });
      }

      private void WindowsFormsHost_Initialized(object sender, EventArgs e) {
         Dispatch();
         textBox.StyleNeeded += TextBox_StyleNeeded;
      }

      private void TextBox_StyleNeeded(object sender, ScintillaNET.StyleNeededEventArgs e) {
         ScintillaNET.Scintilla scintilla = ((ScintillaNET.Scintilla)sender);

         var startPos = scintilla.GetEndStyled();
         var endPos = e.Position;

         scintilla.StartStyling(100);
         scintilla.SetStyling(100, 1);
         scintilla.SetStyling(100, 2);
      }
   }
}
