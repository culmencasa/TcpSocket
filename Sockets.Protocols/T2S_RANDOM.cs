
using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Protocols
{

    public class T2S_RANDOM : IProtocolInfo
    {
        public InnerMsg msg { get; set; } = new InnerMsg();

        public int cmd => (int)ProtocolCmds.T2S_RandomPick;

        public class InnerMsg
        {
            public int RandomNumber { get; set; }
        }

    }
}
