using FastFileReader;
using STextViewControl;
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

namespace VisuPrototype {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window {
      FileWatcher fw;
      object locker = new object();

      ScrollLogic sl;

      public MainWindow() {
         

         InitializeComponent();

         //STextBox.SetVisibleRangeCalculator(VisibleRangeCalculator);
         //STextBox.SetVerticalScrollHandler(VScrollHandler);

         STextBox.Styles[0].Font = "Courier New";
         STextBox.Styles[0].Size = 9;
         STextBox.ReadOnly = true;
      }

      STextBox STextBox => textBox.STextBox;

      private void Lb_EcondingChanged(object sender, Encoding enc) {
         if (Dispatcher.CheckAccess()) {
            encodingLabel.Content = enc.EncodingName;
         } else {
            Dispatcher.BeginInvoke(new Action(() => {
               encodingLabel.Content = enc.EncodingName;
            }));
         }
      }


      private void MiOpenFile_Click(object sender, RoutedEventArgs e) {
         Microsoft.Win32.OpenFileDialog fileBrowserDialog = new Microsoft.Win32.OpenFileDialog();
         fileBrowserDialog.RestoreDirectory = true;
         fileBrowserDialog.Multiselect = false;
         if (fileBrowserDialog.ShowDialog() == true) {
            fw?.Dispose();
            //lb?.Dispose();

            fw = new FileWatcher(fileBrowserDialog.FileName);
            Lb_EcondingChanged(this, fw.Encoding);
            fw.EcondingChanged += Lb_EcondingChanged;
            
            //lb = new LineBuffer(fw);

            sl = new ScrollLogic(fw, textBox.Dispatcher);
            STextBox.ScrollLogic = sl;
            sl.Init(0, Origin.Begin, STextBox.LinesOnScreen);

            //lb.WatchedRangeChanged += Lb_WatchedRangeChanged;

            //lb.MinTimeBetweenUpdates = TimeSpan.FromSeconds(0.1);
            //lb.WatchRange(-1, Origin.End, 100, 100, 1000, 1000);
            //lb.WatchRange(0, Origin.Begin, bufferLines, bufferLines, 1000, 1000);
         }
      }
   }
}
