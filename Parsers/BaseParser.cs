using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PakReader.Parsers
{
    public class BaseParser
    {
        protected BaseParser() { }

        public BaseParser(string path) : this(File.OpenRead(path)) { }
        public BaseParser(Stream stream) : this(new BinaryReader(stream)) { }
        public BaseParser(BinaryReader reader) { }
    }
}
