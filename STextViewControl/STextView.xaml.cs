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
         (double startValue, double endValue) = GetValueRange(hScrollBar);
         sTextBox.SetHScroll(startValue, endValue);
      }
      
      private void VScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
         (double startValue, double endValue) = GetValueRange(vScrollBar);
         sTextBox.SetVScroll(startValue, endValue);
      }

      private (double startValue, double endValue) GetValueRange(System.Windows.Controls.Primitives.ScrollBar scrollBar) {
         double thumbLength = scrollBar.GetThumbLength();
         double thumbCenter = scrollBar.GetThumbCenter();

         double startValue = thumbCenter - thumbLength / 2;
         double endValue = thumbCenter + thumbLength / 2;

         double sv = startValue < 0 ? 0 : startValue > 1 ? 1 : startValue;
         double ev = endValue < 0 ? 0 : endValue > 1 ? 1 : endValue;

         return (sv, ev);
      }

      private void STextBox_HScrollBarValueChanged(object sender, ScrollBarParameter parameter) {
         OnScrollBarValueChanged(hScrollBar, parameter);
      }
      
      private void STextBox_VScrollBarValueChanged(object sender, ScrollBarParameter parameter) {
         OnScrollBarValueChanged(vScrollBar, parameter);
      }

      private void OnScrollBarValueChanged(System.Windows.Controls.Primitives.ScrollBar scrollBar, ScrollBarParameter parameter) {
         if (scrollBar != null) {
            double oldThumbLength = scrollBar.GetThumbLength();
            double oldThumbCenter = scrollBar.GetThumbCenter();
            double newThumbLength = parameter.EndValue - parameter.StartValue;
            double newThumbCenter = (parameter.StartValue + parameter.EndValue) / 2;
            bool newEnabled = (parameter.StartValue > 0 || parameter.EndValue < 1);
            double newMaximuum = newEnabled ? 1 : 0;

            if (oldThumbLength != newThumbLength
                  || oldThumbCenter != newThumbCenter
                  || scrollBar.SmallChange != parameter.SmallChange
                  || scrollBar.LargeChange != parameter.LargeChange
                  || scrollBar.IsEnabled != newEnabled
                  || scrollBar.Maximum != newMaximuum) {
               scrollBar.IsEnabled = newEnabled;
               scrollBar.Maximum = newMaximuum;
               scrollBar.SmallChange = parameter.SmallChange;
               scrollBar.LargeChange = parameter.LargeChange;
               scrollBar.SetThumbLength(newThumbLength);
               scrollBar.SetThumbCenter(newThumbCenter);
            }
         }
      }
   }
}
