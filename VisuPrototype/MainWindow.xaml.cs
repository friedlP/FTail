using FastFileReader;
using System;
using System.Collections.Generic;
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

namespace VisuPrototype {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window {
      FileWatcher fw;
      LineBuffer lb;
      LineRange lineRange;

      public MainWindow() {
         InitializeComponent();
         textBox.Styles[0].Font = "Courier New";
         textBox.Styles[0].Size = 9;
      }

      private void Lb_EcondingChanged(object sender, Encoding enc) {
         if (Dispatcher.CheckAccess()) {
            encodingLabel.Content = enc.EncodingName;
         } else {
            Dispatcher.BeginInvoke(new Action(() => {
               encodingLabel.Content = enc.EncodingName;
            }));
         }
      }

      private void Lb_WatchedRangeChanged(object sender, LineRange range) {
         lb.WatchRange(-1, Origin.End, 100, 100, 1000, 1000);
         lineRange = range;
         UpdateText(lineRange);
      }

      private void UpdateText(LineRange range) {
         if (Dispatcher.CheckAccess()) {
            SetText(range);
         } else {
            Dispatcher.BeginInvoke(new Action(() => {
               SetText(range);
            }));
         }
      }

      private void SetText(LineRange range) {
         int maxLines = textBox.LinesOnScreen;
         StringBuilder stringBuilder = new StringBuilder();
         
         if (range != null) {
            List<FastFileReader.Line> lines = range.AllLines.ToList();

            if (lines.Count > maxLines)
               lines.RemoveRange(0, lines.Count - maxLines);

            lines.ForEach(l => stringBuilder.AppendLine(l.Content.TrimEnd()));

            double relBeginPos = 0;
            double relEndPos = 1;
            if (lines.Count > 0) {
               Extent firstVisibleLineExtent = lines[0].Extent;
               Extent lastVisibleLineExtent = lines[lines.Count - 1].Extent;

               Extent firstLoadedExtent = range.FirstExtent;
               Extent lastLoadedExtent = range.LastExtent;

               int relLineNoFirstVisible = 0;
               int relLineNoLastVisible = 0;
               int n = range.PreviousExtents.Count;
               foreach (var line in range.AllLines) {
                  if (firstVisibleLineExtent == line.Extent)
                     relLineNoFirstVisible = n;
                  if (lastVisibleLineExtent == line.Extent)
                     relLineNoLastVisible = n;
                  ++n;
               }
               n += range.NextExtents.Count;

               double relPosFirstInLoaded = (double)relLineNoFirstVisible / n;
               double relPosLastInLoaded = (double)(relLineNoLastVisible + 1) / n;

               double relPosFirstLoaded = (double)firstLoadedExtent.Begin / range.StreamLength;
               double relPosLastLoaded = (double)lastLoadedExtent.End / range.StreamLength;

               double loadedPart = relPosLastLoaded - relPosFirstLoaded;

               relBeginPos = (1 - loadedPart) * relPosFirstLoaded + loadedPart * relPosFirstInLoaded;
               relEndPos = (1 - loadedPart) * relPosFirstLoaded + loadedPart * relPosLastInLoaded;
            }
            System.Diagnostics.Debug.WriteLine($"Begin: {relBeginPos}, End: {relEndPos}");
         }
         string text = stringBuilder.ToString();
         if (textBox.Text != text) {
            textBox.ReadOnly = false;
            textBox.Text = text;
            textBox.ReadOnly = true;
         }
      }

      private void textBox_SizeChanged(object sender, EventArgs e) {
         UpdateText(lineRange);
         //lb?.ForceUpdate();
      }

      private void miOpenFile_Click(object sender, RoutedEventArgs e) {
         Microsoft.Win32.OpenFileDialog fileBrowserDialog = new Microsoft.Win32.OpenFileDialog();
         fileBrowserDialog.RestoreDirectory = true;
         fileBrowserDialog.Multiselect = false;
         if (fileBrowserDialog.ShowDialog() == true) {
            fw?.Dispose();
            lb?.Dispose();

            fw = new FileWatcher(fileBrowserDialog.FileName);
            lb = new LineBuffer(fw);

            lb.WatchedRangeChanged += Lb_WatchedRangeChanged;
            lb.EcondingChanged += Lb_EcondingChanged;
            lb.MinTimeBetweenUpdates = TimeSpan.FromSeconds(0.1);
            lb.WatchRange(-1, Origin.End, 100, 100, 1000, 1000);
         }
      }
   }
}
