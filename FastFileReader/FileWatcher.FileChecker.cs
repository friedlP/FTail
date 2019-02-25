using System;
using System.IO;
using System.Threading;

namespace FastFileReader
{

   public partial class FileWatcher
   {
      class FileChecker : IDisposable
      {
         DateTime lastWrite;
         FileInfo fileInfo;
         Timer timer;
         TimeSpan checkIntervall;
         volatile bool isActive;

         public event FileSystemEventHandler Changed;

         public FileChecker(string fileName, TimeSpan checkIntervall)
         {
            this.checkIntervall = checkIntervall;
            fileInfo = new FileInfo(fileName);
            lastWrite = fileInfo.LastWriteTimeUtc;
         }

         ~FileChecker()
         {
            Dispose();
         }

         void CallFileCheck()
         {
            if (!isActive)
               return;

            WeakReference weakReference = new WeakReference(this);
            if (timer == null)
            {
               timer = new Timer(new TimerCallback((o) =>
               {
                  FileChecker fc = (FileChecker)weakReference.Target;
                  if (fc != null)
                  {
                     fc.CheckFile();
                     fc.CallFileCheck();
                  }
               }), null, checkIntervall, Timeout.InfiniteTimeSpan);
            }
            else
            {
               timer.Change(checkIntervall, Timeout.InfiniteTimeSpan);
            }
         }

         public void CheckFile()
         {
            if (!isActive)
               return;

            fileInfo.Refresh();
            var lwt = fileInfo.LastWriteTimeUtc;
            if (lwt != lastWrite)
            {
               lastWrite = lwt;
               Changed?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Changed, fileInfo.DirectoryName, fileInfo.Name));
            }
         }

         public void Dispose()
         {
            Stop();
            timer?.Dispose();
            timer = null;
         }

         public void Start()
         {
            if (!isActive)
            {
               isActive = true;
               CallFileCheck();
            }
         }

         public void Stop()
         {
            isActive = false;
         }
      }
   }
}
