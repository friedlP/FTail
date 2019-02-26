using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FastFileReader {

   public partial class FileWatcher : EncodingDetectionReader {
      static int instanceCount;
      string fileName;
      FileSystemWatcher fsw;
      FileChecker fc;

      DateTime encodingValidationTime;
      bool fileModified;
      volatile int fileModificationReported;
      volatile int resetRequired;
      bool disposed;

      protected override Stream GetStream() {
         if (File.Exists(fileName)) {
            return new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
         } else {
            return null;
         }         
      }

      protected override void CloseStream(Stream stream) {
         try {
            if (stream != null) {
               stream.Dispose();
            }
         } catch {
         }
      }

      public FileWatcher(string fileName) {
         ++instanceCount;

         this.fileName = Path.GetFullPath(fileName);
         fsw = new FileSystemWatcher(Path.GetDirectoryName(this.fileName), Path.GetFileName(this.fileName));
         fsw.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
         fsw.Changed += Fsw_Changed;
         fsw.Created += Fsw_Created;
         fsw.Deleted += Fsw_Deleted;
         fsw.Renamed += Fsw_Renamed;
         fsw.Error += Fsw_Error;
         fsw.EnableRaisingEvents = true;

         fc = new FileChecker(this.fileName, new TimeSpan(0, 0, 0, 0, 10));
         fc.Changed += Fsw_Changed;
         fc.Start();
      }

      ~FileWatcher() {
         Dispose(false);
      }

      public override Encoding Encoding {
         get {
            CheckForFileModifications();
            return base.Encoding;
         }
      }

      public override LineRange ReadRange(long position, Origin origin, int maxPrev, int maxNext, int maxPrevExt, int maxNExtExt) {
         CheckForFileModifications();
         return base.ReadRange(position, origin, maxPrev, maxNext, maxPrevExt, maxNExtExt);
      }

      protected override void EncodingValidated() {
         encodingValidationTime = DateTime.UtcNow;
         fileModified = false;
      }

      protected override bool EncodingValidationRequired() {
         return fileModified && DateTime.UtcNow > encodingValidationTime.AddSeconds(.1);
      }

      protected void CheckForFileModifications() {
         if (Interlocked.Exchange(ref fileModificationReported, 0) != 0) {
            fileModified = true;
         }
         if (Interlocked.Exchange(ref resetRequired, 0) != 0) {
            Reset();
         }
      }

      protected override void Reset() {
         base.Reset();

         encodingValidationTime = DateTime.UtcNow;
         fileModified = false;
      }
      
      private void Fsw_Error(object sender, ErrorEventArgs e) {
         resetRequired = 1;
         ReportError(e.GetException());
      }

      private void Fsw_Renamed(object sender, RenamedEventArgs e) {
         resetRequired = 1;
         ReportStreamChanged();
      }

      private void Fsw_Deleted(object sender, FileSystemEventArgs e) {
         resetRequired = 1;
         ReportStreamChanged();
      }

      private void Fsw_Created(object sender, FileSystemEventArgs e) {
         resetRequired = 1;
         ReportStreamChanged();
      }

      private void Fsw_Changed(object sender, FileSystemEventArgs e) {
         fileModificationReported = 1;
         ReportStreamChanged();
      }

      public override void Dispose() {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      // Protected implementation of Dispose pattern.
      protected virtual void Dispose(bool disposing) {
         if (disposed)
            return;

         if (disposing) {
            fsw.EnableRaisingEvents = false;
         }

         fc.Dispose();
         fsw.Dispose();

         --instanceCount;
         System.Diagnostics.Debug.WriteLine("~FileWatcher - Remaining instances: " + instanceCount);

         disposed = true;

         base.Dispose();
      }
   }
}
