using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Protocols
{
    public class S2T_HeartbeatInfo : IProtocolInfo
    {
        public S2T_HeartbeatInfo()
        {
            msg = new InnerMsg();
        }
        public InnerMsg msg { get; set; } = new InnerMsg();

        public class InnerMsg
        {

        }

        public int cmd => (int)ProtocolCmds.S2T_Active;
    }
}
