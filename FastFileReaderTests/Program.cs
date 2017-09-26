using FastFileReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastFileReaderTests {
   class Program {
      static void Main(string[] args) {
         Console.InputEncoding = Encoding.UTF8;
         Console.OutputEncoding = Encoding.UTF8;

         string fName = Path.GetTempFileName();
         FileWatcher fw = new FileWatcher(fName);
         try {
            using (StreamWriter sw = new StreamWriter(fName, false, new UTF8Encoding(false))) {
               Line l = null;
               string line = string.Empty;
               while (true) {
                  ConsoleKeyInfo key = Console.ReadKey();
                  if (key.Key == ConsoleKey.Escape) {
                     break;
                  } else if (key.Key == ConsoleKey.Enter) {
                     sw.WriteLine(line);
                     sw.Flush();
                     l = (l == null) ? fw.GetLine(0) : fw.NextLine(l);
                     if (l != null) {
                        Console.WriteLine();
                        Console.WriteLine($"({l.Begin} - {l.End}) [{fw.Encoding.WebName}]: {l.Content.TrimEnd()}");
                     }
                     line = string.Empty;
                  } else if (key.Key == ConsoleKey.Backspace) {
                     sw.BaseStream.SetLength(0);
                     l = null;
                     line = string.Empty;
                     Console.Clear();
                  } else {
                     line += key.KeyChar;
                  }
               }
            }
         } finally {
            try {
               if (File.Exists(fName)) {
                  File.Delete(fName);
               }
            } catch {
            }
         }
      }
   }
}
