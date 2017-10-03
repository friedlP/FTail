using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FastFileReader;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests {
   [TestClass]
   public class UnitTest1 {
      [TestMethod]
      public void TestMethod1() {
         MemoryStream ms = new MemoryStream();
         StreamWriter sw = new StreamWriter(ms, new UTF8Encoding(false));
         sw.Flush();
         LineReader lr = new LineReader(ms, new UTF8Encoding(false));
         Assert.IsNull(lr.Read(0));
      }

      [TestMethod]
      public void TestMethod2() {
         MemoryStream ms = new MemoryStream();
         StreamWriter sw = new StreamWriter(ms, new UTF8Encoding(false));
         sw.Write("\r");
         sw.Flush();
         LineReader lr = new LineReader(ms, new UTF8Encoding(false));
         Line l = lr.Read(0);
         Assert.AreEqual("\r", l.Content);
         Assert.AreEqual(0, l.Begin);
         Assert.AreEqual(1, l.End);
         Assert.IsNull(lr.Read(2));
      }

      [TestMethod]
      public void TestMethod3() {
         MemoryStream ms = new MemoryStream();
         StreamWriter sw = new StreamWriter(ms, new UTF8Encoding(false));
         sw.Write("\n");
         sw.Flush();
         LineReader lr = new LineReader(ms, new UTF8Encoding(false));
         Line l = lr.Read(0);
         Assert.AreEqual("\n", l.Content);
         Assert.AreEqual(0, l.Begin);
         Assert.AreEqual(1, l.End);
         Assert.IsNull(lr.Read(2));
      }

      [TestMethod]
      public void TestMethod4() {
         MemoryStream ms = new MemoryStream();
         StreamWriter sw = new StreamWriter(ms, new UTF8Encoding(false));
         sw.Write("\r\n");
         sw.Flush();
         LineReader lr = new LineReader(ms, new UTF8Encoding(false));
         Line l = lr.Read(0);
         Assert.AreEqual("\r\n", l.Content);
         Assert.AreEqual(0, l.Begin);
         Assert.AreEqual(2, l.End);
         Assert.IsNull(lr.Read(3));
      }

      void Test(string[] lines, Encoding encoding) {
         MemoryStream ms = new MemoryStream();
         StreamWriter sw = new StreamWriter(ms, encoding);
         var iLines = new(string Content, long Begin, long End)[lines.Length];
         for (int i = 0; i < lines.Length; ++i) {
            long start = ms.Position;
            sw.Write(lines[i]);
            sw.Flush();
            long end = ms.Position;
            iLines[i] = (lines[i], start, end);
         }
         
         LineReader lr = new LineReader(ms, encoding);
         for (int i = 0; i < lines.Length; ++i) {
            Line l = lr.Read(iLines[i].Begin);
            var iLine = iLines[i];
            Assert.AreEqual(iLine.Content, l.Content.Trim(LineReader.BOM));
            Assert.AreEqual(iLine.Begin, l.Begin);
            Assert.AreEqual(iLine.End, l.End);

            l = lr.Read(iLines[i].End - 1);
            Assert.AreEqual(iLine.Content, l.Content.Trim(LineReader.BOM));
            Assert.AreEqual(iLine.Begin, l.Begin);
            Assert.AreEqual(iLine.End, l.End);
         }

      }

      [TestMethod]
      public void TestMethod5() {
         Test(new string[] { "\r\n", "\r\n" }, new UTF8Encoding(false));
      }

      [TestMethod]
      public void TestMethod6() {
         Test(new string[] { "\r\n", "\r\n" }, new UnicodeEncoding(false, false));
      }
      
      [TestMethod]
      public void TestMethod7() {
         Test(new string[] { "\r\n", "\r\n" }, new UnicodeEncoding(true, false));
      }

      [TestMethod]
      public void TestMethod8() {
         Test(new string[] { "\r\n", "\r\n" }, new UTF32Encoding(false, false));
      }

      [TestMethod]
      public void TestMethod9() {
         Test(new string[] { "\r\n", "\r\n" }, new UTF32Encoding(true, false));
      }


      [TestMethod]
      public void TestMethod5bom() {
         Test(new string[] { "\r\n", "\r\n" }, new UTF8Encoding(true));
      }

      [TestMethod]
      public void TestMethod6bom() {
         Test(new string[] { "\r\n", "\r\n" }, new UnicodeEncoding(false, true));
      }

      [TestMethod]
      public void TestMethod7bom() {
         Test(new string[] { "\r\n", "\r\n" }, new UnicodeEncoding(true, true));
      }

      [TestMethod]
      public void TestMethod8bom() {
         Test(new string[] { "\r\n", "\r\n" }, new UTF32Encoding(false, true));
      }

      [TestMethod]
      public void TestMethod9bom() {
         Test(new string[] { "\r\n", "\r\n" }, new UTF32Encoding(true, true));
      }

      [TestMethod]
      public void TestMethod10() {
         Test(new string[] { "\r\n", "\r\n" }, new ASCIIEncoding());
      }

      [TestMethod]
      public void TestMethod11() {
         Test(new string[] { "\r\n", "\r\n" }, Encoding.GetEncoding("Windows-1252"));
      }


      [TestMethod]
      public void TestMethod12() {
         Test(new string[] { "\u2028", "\r\n", "\u2028" }, new UTF8Encoding(false));
      }

      [TestMethod]
      public void TestMethod13() {
         Test(new string[] { "\u2028", "\r\n", "\u2028" }, new UnicodeEncoding(false, false));
      }

      [TestMethod]
      public void TestMethod14() {
         Test(new string[] { "\u2028", "\r\n", "\u2028" }, new UnicodeEncoding(true, false));
      }

      [TestMethod]
      public void TestMethod15() {
         Test(new string[] { "\u2028", "\r\n", "\u2028" }, new UTF32Encoding(false, false));
      }

      [TestMethod]
      public void TestMethod16() {
         Test(new string[] { "\u2028", "\r\n", "\u2028" }, new UTF32Encoding(true, false));
      }

      [TestMethod]
      public void TestMethod17() {
         Test(new string[] { "\n", "\r\n", "\n" }, new ASCIIEncoding());
      }

      [TestMethod]
      public void TestMethod18() {
         Test(new string[] { "\r", "\r\n", "\r" }, new ASCIIEncoding());
      }

      [TestMethod]
      public void TestMethod19() {
         Test(new string[] { " \n", " \n", " \r" }, new ASCIIEncoding());
      }

      [TestMethod]
      public void TestMethod20() {
         Test(new string[] { " \r", " \n" }, new ASCIIEncoding());
      }

      [TestMethod]
      public void TestMethod21() {
         Test(new string[] { "\u2028", "\u2028", "\u2028" }, new UTF8Encoding(false));
      }

   }
}
