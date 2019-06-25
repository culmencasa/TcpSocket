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
    public class S2T_QuizCommit : IProtocolInfo
    {
        public S2T_QuizCommit()
        {
            msg = new InnerMsg();
        }
        public InnerMsg msg { get; set; } = new InnerMsg();

        public class InnerMsg
        {
            /// <summary>
            /// 学生ID
            /// </summary>
            public string sId { get; set; }

            public string cId { get; set; }

            public string imgUrl { get; set; }

            public string busId { get; set; }
        }

        public int cmd => (int)ProtocolCmds.S2T_QuizCommit;
    }
}
