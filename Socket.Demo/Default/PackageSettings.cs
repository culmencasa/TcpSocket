using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sockets.Default
{
    public class PackageSettings : IPackageSettings
    {
        public string PrefixString
        {
            get;
            set;
        }

        public string LengthString
        {
            get;
            set;
        }

        public Encoding Encoding
        {
            get;
            set;
        }

        public int MaxConnectionNumber
        {
            get;
            set;
        }
        public int BufferSize { get; set; }
    }
}
