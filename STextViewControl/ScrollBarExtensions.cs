using System.Windows.Controls.Primitives;

namespace STextViewControl {
   public static class ScrollBarExtensions {
      public static double GetThumbCenter(this ScrollBar s) {
         double thumbLength = GetThumbLength(s);
         double trackLength = s.Maximum - s.Minimum;

         return thumbLength / 2 + s.Minimum + (s.Value - s.Minimum) *
             (trackLength - thumbLength) / trackLength;
      }

      public static void SetThumbCenter(this ScrollBar s, double thumbCenter) {
         double thumbLength = GetThumbLength(s);
         double trackLength = s.Maximum - s.Minimum;

         if (thumbCenter >= s.Maximum - thumbLength / 2) {
            s.Value = s.Maximum;
         } else if (thumbCenter <= s.Minimum + thumbLength / 2) {
            s.Value = s.Minimum;
         } else if (thumbLength >= trackLength) {
            s.Value = s.Minimum;
         } else {
            s.Value = s.Minimum + trackLength *
                ((thumbCenter - s.Minimum - thumbLength / 2)
                / (trackLength - thumbLength));
         }
      }

      public static double GetThumbLength(this ScrollBar s) {
         double trackLength = s.Maximum - s.Minimum;
         return trackLength * s.ViewportSize /
           (trackLength + s.ViewportSize);
      }

      public static void SetThumbLength(this ScrollBar s, double thumbLength) {
         double trackLength = s.Maximum - s.Minimum;

         if (thumbLength < 0) {
            s.ViewportSize = 0;
         } else if (thumbLength < trackLength) {
            s.ViewportSize = trackLength * thumbLength / (trackLength - thumbLength);
         } else {
            s.ViewportSize = double.MaxValue;
         }
      }
   }
}
