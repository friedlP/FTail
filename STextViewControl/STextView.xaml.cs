using ScintillaNET;
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

namespace STextViewControl {
   /// <summary>
   /// Interaction logic for UserControl1.xaml
   /// </summary>
   public partial class STextView : UserControl {
      public event EventHandler<UpdateUIEventArgs> UpdateUI;

      public STextView() {
         InitializeComponent();
      }

      private void STextBox_UpdateUI(object sender, ScintillaNET.UpdateUIEventArgs e) {
         UpdateUI?.Invoke(this, e);
      }

      public STextBox STextBox => sTextBox;

      private void HScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
         double thumbLength = hScrollBar.GetThumbLength();
         double thumbCenter = hScrollBar.GetThumbCenter();

         double startValue = thumbCenter - thumbLength / 2;
         double endValue = thumbCenter + thumbLength / 2;
         
         double sv = startValue < 0 ? 0 : startValue > 1 ? 1 : startValue;
         double ev = endValue < 0 ? 0 : endValue > 1 ? 1 : endValue;

         sTextBox.SetHScroll(sv, ev);
      }


      private void VScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
         double thumbLength = vScrollBar.GetThumbLength();
         double thumbCenter = vScrollBar.GetThumbCenter();

         double startValue = thumbCenter - thumbLength / 2;
         double endValue = thumbCenter + thumbLength / 2;

         double sv = startValue < 0 ? 0 : startValue > 1 ? 1 : startValue;
         double ev = endValue < 0 ? 0 : endValue > 1 ? 1 : endValue;

         sTextBox.SetVScroll(sv, ev);
      }

      private void STextBox_HScrollBarValueChanged(object sender, ScrollBarParameter parameter) {
         if (hScrollBar != null) {
            double oldThumbLength = hScrollBar.GetThumbLength();
            double oldThumbCenter = hScrollBar.GetThumbCenter();
            double newThumbLength = parameter.EndValue - parameter.StartValue;
            double newThumbCenter = (parameter.StartValue + parameter.EndValue) / 2;
            bool newEnabled = (parameter.StartValue > 0 || parameter.EndValue < 1);
            double newMaximuum = newEnabled ? 1 : 0;

            if (oldThumbLength != newThumbLength
                  || oldThumbCenter != newThumbCenter
                  || hScrollBar.SmallChange != parameter.SmallChange
                  || hScrollBar.LargeChange != parameter.LargeChange
                  || hScrollBar.IsEnabled != newEnabled
                  || hScrollBar.Maximum != newMaximuum) {
               hScrollBar.IsEnabled = newEnabled;
               hScrollBar.Maximum = newMaximuum;
               hScrollBar.SmallChange = parameter.SmallChange;
               hScrollBar.LargeChange = parameter.LargeChange;
               hScrollBar.SetThumbLength(newThumbLength);
               hScrollBar.SetThumbCenter(newThumbCenter);
            }
         }
      }


      private void STextBox_VScrollBarValueChanged(object sender, ScrollBarParameter parameter) {
         if (vScrollBar != null) {
            double oldThumbLength = vScrollBar.GetThumbLength();
            double oldThumbCenter = vScrollBar.GetThumbCenter();
            double newThumbLength = parameter.EndValue - parameter.StartValue;
            double newThumbCenter = (parameter.StartValue + parameter.EndValue) / 2;
            bool newEnabled = (parameter.StartValue > 0 || parameter.EndValue < 1);
            double newMaximuum = newEnabled ? 1 : 0;

            if (oldThumbLength != newThumbLength
                  || oldThumbCenter != newThumbCenter
                  || vScrollBar.SmallChange != parameter.SmallChange
                  || vScrollBar.LargeChange != parameter.LargeChange
                  || vScrollBar.IsEnabled != newEnabled
                  || vScrollBar.Maximum != newMaximuum) {
               vScrollBar.IsEnabled = newEnabled;
               vScrollBar.Maximum = newMaximuum;
               vScrollBar.SmallChange = parameter.SmallChange;
               vScrollBar.LargeChange = parameter.LargeChange;
               vScrollBar.SetThumbLength(newThumbLength);
               vScrollBar.SetThumbCenter(newThumbCenter);
            }
         }
      }
   }
}
