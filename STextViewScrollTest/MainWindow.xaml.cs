using STextViewControl;
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

namespace STextViewScrollTest {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window {
      long minLine;
      long maxLine;
      long firstLine;

      public MainWindow() {
         InitializeComponent();

         minLine = -200;
         maxLine = 200;
         firstLine = 0;

         //textBox.STextBox.SetVisibleRangeCalculator(VisibleRangeCalculator);
         //textBox.STextBox.SetVerticalScrollHandler(VScrollHandler);

         VScrollHandler(textBox.STextBox, 0, 0);
      }

      (double min, double max) VisibleRangeCalculator(int lines, int firstVisibleLine, int linesOnScreen) {
         double linesTotal = maxLine - minLine + 1;
         double maxOffset = linesTotal;
         if (maxOffset <= 0) {
            return (0, 1);
         }
         long fl = firstLine - minLine;
         double min = fl / maxOffset;
         double max = (fl + linesOnScreen) / maxOffset;
         return (min, max);
      }
      void VScrollHandler(STextBox sTextBox, double scroll, long scrollLines) {
         int visLines = sTextBox.LinesOnScreen;
         firstLine = firstLine + scrollLines;
         if (firstLine + visLines > maxLine)
            firstLine = maxLine - visLines + 1;
         if (firstLine < minLine)
            firstLine = minLine;
         string text = string.Empty;
         for (int i = 0; i < visLines + 1 && firstLine + i <= maxLine; i++) {
            text += $"{firstLine + i}: Test" + Environment.NewLine;
         }
         text.TrimEnd(new char[] { '\r', '\n' });
         sTextBox.Document = new ScintillaNET.Document();
         sTextBox.Text = text;

      }
   }
}
