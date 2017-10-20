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
      int maxPrev;
      int maxFoll;
      LineRange curState;
      DateTime lastUpdate;
      bool updateScheduled;
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
         reader.StreamChanged += Reader_StreamChangee;
      }

      private void Reader_StreamChangee(object sender) {
         CheckRange();
      }

      private void CheckRange() {
         lock (lockObject) {
            if (!updateScheduled) {
               updateScheduled = true;
               DateTime now = DateTime.UtcNow;
               TimeSpan sleep = (lastUpdate + MinTimeBetweenUpdates) - now;
               Task.Run(() => {
                  if (sleep.Ticks > 0)
                     Thread.Sleep(sleep);
                  lock (lockObject) {
                     if (position >= 0) {
                        LineRange range = reader.ReadRange(position, maxPrev, maxFoll);

                        if (range != curState) {
                           curState = range;
                           WatchedRangeChanged?.Invoke(this, curState);
                        }
                     } else {
                        curState = null;
                     }
                     lastUpdate = DateTime.UtcNow;
                     updateScheduled = false;
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

      public void WatchRange(long position, int maxPrev, int maxFoll) {
         lock (lockObject) {
            this.position = position;
            this.maxPrev = maxPrev;
            this.maxFoll = maxFoll;

            CheckRange();
         }
      }

   }
}
