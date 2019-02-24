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
         volatile bool isActive;

         public event FileSystemEventHandler Changed;

         public FileChecker(string fileName)
         {
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
            timer?.Dispose();
            timer = new Timer(new TimerCallback((o) =>
            {
               FileChecker fc = (FileChecker)weakReference.Target;
               if (fc != null)
               {
                  fc.CheckFile();
                  fc.CallFileCheck();
               }
            }), null, 10, Timeout.Infinite);
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
