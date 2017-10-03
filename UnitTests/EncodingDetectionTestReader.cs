using FastFileReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace UnitTests {
   class EncodingDetectionTestReader : EncodingDetectionReader {
      bool validationRequired;

      public MemoryStream Stream { get; } = new MemoryStream();

      protected override Stream GetStream() {
         return Stream;
      }

      public void SetValidationRequired() {
         validationRequired = true;
      }

      protected override void EncodingValidated() {
         validationRequired = false;
      }

      protected override bool EncodingValidationRequired() {
         return validationRequired;
      }

      protected override void Reset() {
         base.Reset();
         validationRequired = false;
      }
   }
}
