using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Protocols
{

    public class T2S_Catchphrase : IProtocolInfo
    {
        public T2S_Catchphrase()
        {
            msg = new InnerMsg();
        }
        public InnerMsg msg { get; set; }

        public class InnerMsg
        {
            /// <summary>
            /// 题目id
            /// </summary>
            public string questionId { get; set; }
            /// <summary>
            /// 班级id
            /// </summary>
            public string classId { get; set; }
        }

        public int cmd => (int)ProtocolCmds.T2S_Catchphrase;
    }
}
