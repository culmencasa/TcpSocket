using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Protocols
{

    /// <summary>
    /// 学生连接消息
    /// </summary>
    public class S2T_ConnectInfo : IProtocolInfo
    {
        public S2T_ConnectInfo()
        {
            msg = new InnerMsg();
        }
        public InnerMsg msg { get; set; }

        public class InnerMsg
        {
            /// <summary>
            /// 学生ID
            /// </summary>
            public string sId { get; set; }
            /// <summary>
            /// 班级ID
            /// </summary>
            public string cId { get; set; }
        }

        public int cmd => (int)ProtocolCmds.S2T_Connect;
    }
}
