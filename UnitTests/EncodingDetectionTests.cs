using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using FastFileReader;

namespace UnitTests {
   [TestClass]
   public class EncodingDetectionTests {
      Line ReadLine(EncodingDetectionTestReader reader, long position) {
         long sPos = reader.Stream.Position;
         Line l = reader.GetLine(position);
         reader.Stream.Seek(sPos, SeekOrigin.Begin);
         return l;
      }

      [TestMethod]
      public void TestMethod1() {
         EncodingDetectionTestReader tr = new EncodingDetectionTestReader();
         StreamWriter sw = new StreamWriter(tr.Stream, new UTF8Encoding(false));
         sw.Write("A");
         sw.Flush();
         tr.GetLine(0);
         Assert.AreEqual(Encoding.ASCII.CodePage, tr.Encoding.CodePage);
      }

      [TestMethod]
      public void TestMethod2() {
         EncodingDetectionTestReader tr = new EncodingDetectionTestReader();
         StreamWriter sw = new StreamWriter(tr.Stream, new UTF8Encoding(false));
         sw.Write("Ä");
         sw.Flush();
         tr.GetLine(0);
         Assert.AreEqual(Encoding.UTF8.CodePage, tr.Encoding.CodePage);
      }

      [TestMethod]
      public void TestMethod3() {
         EncodingDetectionTestReader tr = new EncodingDetectionTestReader();
         StreamWriter sw = new StreamWriter(tr.Stream, new UTF8Encoding(false));
         sw.Write("A");
         sw.Flush();
         ReadLine(tr, 0);
         Assert.AreEqual(Encoding.ASCII.CodePage, tr.Encoding.CodePage);
         sw.Write("Ä");
         sw.Flush();
         ReadLine(tr, 0);
         Assert.AreEqual(Encoding.UTF8.CodePage, tr.Encoding.CodePage);
      }

      [TestMethod]
      public void TestMethod4() {
         EncodingDetectionTestReader tr = new EncodingDetectionTestReader();
         StreamWriter sw = new StreamWriter(tr.Stream, new UTF8Encoding(false));
         sw.Write("AÄ");
         sw.Flush();
         ReadLine(tr, 0);
         Assert.AreEqual(Encoding.UTF8.CodePage, tr.Encoding.CodePage);
         sw.BaseStream.SetLength(1);
         ReadLine(tr, 0);
         Assert.AreEqual(Encoding.ASCII.CodePage, tr.Encoding.CodePage);
      }

      [TestMethod]
      public void TestMethod5() {
         EncodingDetectionTestReader tr = new EncodingDetectionTestReader();
         StreamWriter sw = new StreamWriter(tr.Stream, new UTF8Encoding(false));
         sw.Write("AÄ");
         sw.Flush();
         ReadLine(tr, 0);
         Assert.AreEqual(Encoding.UTF8.CodePage, tr.Encoding.CodePage);
         sw.BaseStream.Seek(1, SeekOrigin.Begin);
         sw.Write("AA");
         sw.Flush();
         ReadLine(tr, 0);
         Assert.AreEqual(Encoding.UTF8.CodePage, tr.Encoding.CodePage);
         tr.SetValidationRequired();
         ReadLine(tr, 0);
         Assert.AreEqual(Encoding.ASCII.CodePage, tr.Encoding.CodePage);
      }

      [TestMethod]
      public void TestMethod6() {
         EncodingDetectionTestReader tr = new EncodingDetectionTestReader();
         StreamWriter sw = new StreamWriter(tr.Stream, new UTF8Encoding(false));
         sw.Write(new String('A', 128 * 1024 -1));
         sw.Flush();
         ReadLine(tr, 0);
         Assert.AreEqual(Encoding.ASCII.CodePage, tr.Encoding.CodePage);
         sw.Write("Ä");
         sw.Flush();
         ReadLine(tr, 0);
         Assert.AreEqual(Encoding.UTF8.CodePage, tr.Encoding.CodePage);
      }

      [TestMethod]
      public void TestMethod7() {
         EncodingDetectionTestReader tr = new EncodingDetectionTestReader();
         StreamWriter sw = new StreamWriter(tr.Stream, new UTF8Encoding(false));
         sw.Write(new String('A', 128 * 1024));
         sw.Flush();
         ReadLine(tr, 0);
         Assert.AreEqual(Encoding.ASCII.CodePage, tr.Encoding.CodePage);
         sw.Write("Ä");
         sw.Flush();
         Line l = ReadLine(tr, 0);
         Assert.AreEqual(Encoding.UTF8.CodePage, tr.Encoding.CodePage);
      }
   }
}
