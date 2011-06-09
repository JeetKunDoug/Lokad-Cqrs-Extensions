using System;
using System.Diagnostics;
using System.IO;

namespace Shared
{
    public class OneTimeTextWriterTraceListener : TextWriterTraceListener
    {
        public OneTimeTextWriterTraceListener(string fileName)
            : this(fileName, fileName)
        {
        }

        public OneTimeTextWriterTraceListener(string fileName, string name) : base(fileName, name)
        {
            string path = Path.GetFullPath(fileName);
            var directoryName = Path.GetDirectoryName(path);

            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            if(File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}