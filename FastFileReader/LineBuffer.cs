using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FastFileReader {

   public delegate void WatchedRangeChangedHandler(object sender, LineRange range);
   
   public class LineBuffer {
      EncodingDetectionReader reader;
      long position = -1;
      Origin origin = Origin.Begin;
      int maxPrev;
      int maxFoll;
      LineRange curState;
      DateTime lastUpdate;
      bool updateScheduled;
      bool updateForced;
      TimeSpan minTimeBetweenUpdates = TimeSpan.FromMilliseconds(1);
      object lockObject = new object();

      public TimeSpan MinTimeBetweenUpdates {
         get {
            return minTimeBetweenUpdates;
         }
         set {
            minTimeBetweenUpdates = value.TotalMilliseconds > 1 ? value : TimeSpan.FromMilliseconds(1);
         }
      }

      public event WatchedRangeChangedHandler WatchedRangeChanged;
      public event EncodingChangedEventHandler EcondingChanged;

      public LineBuffer(EncodingDetectionReader reader) {
         this.reader = reader;
         reader.Error += Reader_Error;
         reader.EcondingChanged += Reader_EcondingChanged;
         reader.StreamChanged += Reader_StreamChanged;
      }

      private void Reader_StreamChanged(object sender) {
         CheckRange();
      }

      private void CheckRange() {
         lock (lockObject) {
            System.Diagnostics.Debug.WriteLine("Check requested");
            if (!updateScheduled) {
               updateScheduled = true;
               DateTime now = DateTime.UtcNow;
               TimeSpan sleep = (lastUpdate + MinTimeBetweenUpdates) - now;
               System.Diagnostics.Debug.WriteLine("Check scheduled");
               Task.Run(() => {
                  if (sleep.Ticks > 0)
                     Thread.Sleep(sleep);
                  lock (lockObject) {
                     System.Diagnostics.Debug.WriteLine("Execute check");
                     lastUpdate = DateTime.UtcNow;
                     updateScheduled = false;
                     if (position >= 0 && origin == Origin.Begin || position < 0 && origin == Origin.End) {
                        LineRange range = reader.ReadRange(position, origin, maxPrev, maxFoll);
                        
                        if (range != curState || updateForced) {
                           updateForced = false;
                           curState = range;
                           System.Diagnostics.Debug.WriteLine("Invoke listener");
                           WatchedRangeChanged?.Invoke(this, curState);
                        }
                     } else {
                        curState = null;
                     }
                  }
               });
            }
         }
      }

      private void Reader_EcondingChanged(object sender, Encoding enc) {
         EcondingChanged?.Invoke(this, enc);
      }

      private void Reader_Error(object sender, Exception e) {
      }

      public void ForceUpdate() {
         lock (lockObject) {
            updateForced = true;
            CheckRange();
         }
      }

      public void WatchRange(long position, Origin origin, int maxPrev, int maxFoll) {
         lock (lockObject) {
            this.position = position;
            this.origin = origin;
            this.maxPrev = maxPrev;
            this.maxFoll = maxFoll;

            CheckRange();
         }
      }

   }
}
