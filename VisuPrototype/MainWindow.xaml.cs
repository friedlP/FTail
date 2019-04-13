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
      ScrollLogic sl;
      bool followTail;

      public MainWindow() {
         

         InitializeComponent();

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

            fw = new FileWatcher(fileBrowserDialog.FileName);
            Lb_EcondingChanged(this, fw.Encoding);
            fw.EcondingChanged += Lb_EcondingChanged;

            sl = new ScrollLogic(fw, textBox.Dispatcher);
            STextBox.ScrollLogic = sl;
            sl.FollowTail = followTail;
            eofMarker.Fill = new SolidColorBrush(Color.FromRgb(127, 127, 127));
            sl.IsAtEndOfFileChanged += (_, isAtEnd) => {
               eofMarker.Fill = new SolidColorBrush(isAtEnd ? Color.FromRgb(0, 0, 255) : Color.FromRgb(127, 127, 127));
            };
            sl.Init(0, Origin.Begin, STextBox.LinesOnScreen);
         }
      }

      void SetFllowTail(bool follow)
      {
         followTail = follow;
         miFollowTailYes.IsChecked = follow;
         miFollowTailNo.IsChecked = !follow;
         if (sl != null)
            sl.FollowTail = follow;
      }

      private void MiFollowTailNo_Click(object sender, RoutedEventArgs e)
      {
         SetFllowTail(false);
      }

      private void MiFollowTailYes_Click(object sender, RoutedEventArgs e)
      {
         SetFllowTail(true);
      }
   }
}
