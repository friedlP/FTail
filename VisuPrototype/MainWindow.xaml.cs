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
      public MainWindow() {
         InitializeComponent();

         FileWatcher fw = new FileWatcher(@"C:\temp\ftail_test.txt");
         LineBuffer lb = new LineBuffer(fw);

         lb.WatchedRangeChanged += Lb_WatchedRangeChanged;
         lb.EcondingChanged += Lb_EcondingChanged;
         lb.MinTimeBetweenUpdates = TimeSpan.FromSeconds(0.01);
         lb.WatchRange(0, 10, 10);
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
         StringBuilder stringBuilder = new StringBuilder();
         if (range != null) {
            range.PreviousLines?.ForEach(l => stringBuilder.AppendLine("P: " + l.Content.TrimEnd()));
            if (range.RequestedLine != null) stringBuilder.AppendLine("C: " + range.RequestedLine.Content.TrimEnd());
            range.NextLines?.ForEach(l => stringBuilder.AppendLine("N: " + l.Content.TrimEnd()));
         }

         if (Dispatcher.CheckAccess()) {
            textBox.Text = stringBuilder.ToString();
         } else {
            Dispatcher.BeginInvoke(new Action(() => {
               textBox.Text = stringBuilder.ToString();
            }));
         }
      }
   }
}
