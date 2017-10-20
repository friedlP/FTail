using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FastFileReader {

   public class FileWatcher : EncodingDetectionReader {
      static int instanceCount;
      string fileName;
      FileSystemWatcher fsw;

      DateTime encodingValidationTime;
      bool fileModified;
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

         CheckCyclic();
      }

      ~FileWatcher() {
         --instanceCount;
         System.Diagnostics.Debug.WriteLine("~FileWatcher - Remaining instances: " + instanceCount);
      }

      private void CheckCyclic() {
         Task.Run(() => {
            if (disposed)
               return;

            System.Threading.Thread.Sleep(250);
            fileModified = true;
            HandleStreamChanged();
            CheckCyclic();
         });
      }

      protected override void EncodingValidated() {
         encodingValidationTime = DateTime.UtcNow;
         fileModified = false;
      }

      protected override bool EncodingValidationRequired() {
         return fileModified && DateTime.UtcNow > encodingValidationTime.AddSeconds(1);
      }

      protected override void Reset() {
         base.Reset();

         encodingValidationTime = DateTime.UtcNow;
         fileModified = false;
      }
      
      private void Fsw_Error(object sender, ErrorEventArgs e) {
         HandleError(e.GetException());
      }

      private void Fsw_Renamed(object sender, RenamedEventArgs e) {
         Reset();
         HandleStreamChanged();
      }

      private void Fsw_Deleted(object sender, FileSystemEventArgs e) {
         Reset();
         HandleStreamChanged();
      }

      private void Fsw_Created(object sender, FileSystemEventArgs e) {
         Reset();
         HandleStreamChanged();
      }

      private void Fsw_Changed(object sender, FileSystemEventArgs e) {
         fileModified = true;
         HandleStreamChanged();
      }

      public override void Dispose() {
         fsw.EnableRaisingEvents = false;
         fsw.Dispose();
         base.Dispose();
         disposed = true;
      }
   }
}
