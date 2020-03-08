using System;
using System.IO;
using System.Text;

namespace ModConsole
{
    internal class LambdaWriter : TextWriter
    {
        private const string ERROR = "#ff5370";
        private const string WARNING = "yellow";
        
        private readonly Action<string> _printer;

        public LambdaWriter(Action<string> printer) => _printer = printer;

        public override Encoding Encoding => Encoding.Default;

        public override void WriteLine(string value) => _printer($"<color={(value.Contains("warning") ? WARNING : ERROR)}>{value}</color>");
    }
}