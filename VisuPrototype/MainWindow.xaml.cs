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

         fw = new FileWatcher(@"C:\temp\ftail_test.txt");
         lb = new LineBuffer(fw);

         lb.WatchedRangeChanged += Lb_WatchedRangeChanged;
         lb.EcondingChanged += Lb_EcondingChanged;
         lb.MinTimeBetweenUpdates = TimeSpan.FromSeconds(0.1);
         lb.WatchRange(-1, Origin.End, 100, 100);
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
         lb.WatchRange(-1, Origin.End, 100, 100);
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

            //range.PreviousLines?.Skip(100 - maxLines + 1).ToList().ForEach(l => stringBuilder.AppendLine("P: " + l.Content.TrimEnd()));
            //if (range.RequestedLine != null) stringBuilder.AppendLine("C: " + range.RequestedLine.Content.TrimEnd());
            ////range.NextLines?.ForEach(l => stringBuilder.AppendLine("N: " + l.Content.TrimEnd()));
            List<FastFileReader.Line> lines = new List<FastFileReader.Line>();
            if (range.PreviousLines != null) lines.AddRange(range.PreviousLines);
            if (range.RequestedLine != null) lines.Add(range.RequestedLine);
            if (range.NextLines != null) lines.AddRange(range.NextLines);

            if (lines.Count > maxLines)
               lines.RemoveRange(0, lines.Count - maxLines);

            lines.ForEach(l => stringBuilder.AppendLine(l.Content.TrimEnd()));
         }
         string text = stringBuilder.ToString();
         if (textBox.Text != text) textBox.Text = text;
      }

      private void textBox_SizeChanged(object sender, EventArgs e) {
         UpdateText(lineRange);
         //lb?.ForceUpdate();
      }
   }
}
