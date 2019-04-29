using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FastFileReader
{

   public delegate void WatchedRangeChangedHandler(object sender, LineRange range);

   public class LineBuffer : IDisposable
   {
      static int instanceCount;
      EncodingDetectionReader reader;
      long position = -1;
      Origin origin = Origin.Begin;
      int maxPrev;
      int maxFoll;
      int maxPrevExtent;
      int maxFollExtent;
      Origin watchOrigin = Origin.Begin;
      LineRange curState;
      DateTime lastUpdate;
      bool updateScheduled;
      bool updateForced;
      bool fullUpdate;
      bool disposed;
      TimeSpan minTimeBetweenUpdates = TimeSpan.FromMilliseconds(1);
      Timer timer;
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

      public LineBuffer(EncodingDetectionReader reader)
      {
         ++instanceCount;
         this.reader = reader;
         reader.Error += Reader_Error;
         reader.EcondingChanged += Reader_EcondingChanged;
         reader.StreamChanged += Reader_StreamChanged;
      }

      ~LineBuffer()
      {
         Dispose(false);
      }

      private void Reader_StreamChanged(object sender, WatcherChangeTypes changeType)
      {
         CheckRange(changeType != WatcherChangeTypes.Changed);
      }

      private void CheckRange(bool fullUpdate)
      {
         lock (lockObject)
         {
            if (disposed)
               return;

            this.fullUpdate |= fullUpdate;

            //System.Diagnostics.Debug.WriteLine("Check requested");
            if (!updateScheduled)
            {
               updateScheduled = true;
               DateTime now = DateTime.UtcNow;
               TimeSpan sleep = (lastUpdate + MinTimeBetweenUpdates) - now;
               //System.Diagnostics.Debug.WriteLine("Check scheduled");
               if (sleep <= TimeSpan.Zero)
               {
                  Update(true);
               }
               else if (timer == null)
               {
                  timer = new Timer((state) => Update(true), null, sleep, Timeout.InfiniteTimeSpan);
               }
               else
               {
                  timer.Change(sleep, Timeout.InfiniteTimeSpan);
               }
            }
         }
      }

      private void Update(bool triggerEvent)
      {
         lock (lockObject)
         {
            if (disposed)
               return;

            //System.Diagnostics.Debug.WriteLine("Execute check");
            lastUpdate = DateTime.UtcNow;
            updateScheduled = false;
            if (position >= 0 && origin == Origin.Begin || position < 0 && origin == Origin.End)
            {
               LineRange range = reader.ReadRange(position, origin, maxPrev, maxFoll, maxPrevExtent, maxFollExtent, fullUpdate ? null : curState);

               fullUpdate = false;
               if (origin == Origin.Begin && watchOrigin == Origin.End)
               {
                  position -= range.StreamLength;
                  origin = Origin.End;
               } 
               else if (origin == Origin.End && watchOrigin == Origin.Begin)
               {
                  position += range.StreamLength;
                  origin = Origin.Begin;
               }

               if (range != curState || updateForced)
               {
                  updateForced = false;
                  curState = range;
                  //System.Diagnostics.Debug.WriteLine("Invoke listener");
                  if (triggerEvent)
                  {
                     WatchedRangeChanged?.Invoke(this, curState);
                  }
               }
            }
            else
            {
               curState = null;
            }
         }
      }

      private void Reader_EcondingChanged(object sender, Encoding enc)
      {
         lock (lockObject)
         {
            fullUpdate = true;
         }
         EcondingChanged?.Invoke(this, enc);
      }

      private void Reader_Error(object sender, Exception e)
      {
      }

      public void ForceUpdate(bool fullUpdate)
      {
         lock (lockObject)
         {
            updateForced = true;
            CheckRange(fullUpdate);
         }
      }

      public void WatchRange(long position, Origin origin, int maxPrev, int maxFoll, int maxPrevExtent, int maxFollExtent, Origin watchOrigin)
      {
         lock (lockObject)
         {
            SetRange(position, origin, maxPrev, maxFoll, maxPrevExtent, maxFollExtent, watchOrigin);

            CheckRange(false);
         }
      }

      public LineRange ReadRange(long position, Origin origin, int maxPrev, int maxFoll, int maxPrevExtent, int maxFollExtent, Origin watchOrigin)
      {
         lock (lockObject)
         {
            SetRange(position, origin, maxPrev, maxFoll, maxPrevExtent, maxFollExtent, watchOrigin);
            Update(false);
            return curState;
         }
      }

      public LineRange FetchRange(long position, Origin origin, int maxPrev, int maxFoll, int maxPrevExtent, int maxFollExtent)
      {
         lock (lockObject)
         {
            if (position >= 0 && origin == Origin.Begin || position < 0 && origin == Origin.End)
            {
               return reader.ReadRange(position, origin, maxPrev, maxFoll, maxPrevExtent, maxFollExtent);
            }
            else
            {
               return new LineRange();
            }
         }
      }

      private void SetRange(long position, Origin origin, int maxPrev, int maxFoll, int maxPrevExtent, int maxFollExtent, Origin watchOrigin)
      {
         this.position = position;
         this.origin = origin;
         this.maxPrev = maxPrev;
         this.maxFoll = maxFoll;
         this.maxPrevExtent = maxPrevExtent;
         this.maxFollExtent = maxFollExtent;
         this.watchOrigin = watchOrigin;
      }

      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      // Protected implementation of Dispose pattern.
      protected virtual void Dispose(bool disposing)
      {
         if (disposed)
            return;

         if (disposing)
         {
            reader.Error -= Reader_Error;
            reader.EcondingChanged -= Reader_EcondingChanged;
            reader.StreamChanged -= Reader_StreamChanged;
         }

         timer?.Dispose();
         timer = null;

         --instanceCount;
         System.Diagnostics.Debug.WriteLine("~LineBuffer - Remaining instances: " + instanceCount);

         disposed = true;
      }

      public string Fetch(long beginPosFirstLine, int beginCol, long endPosLastLine, int endCol)
      {
         if (beginPosFirstLine >= endPosLastLine)
            return String.Empty;

         lock (lockObject)
         {
            return reader.FetchRange(beginPosFirstLine, beginCol, endPosLastLine, endCol);
         }
      }
   }
}
