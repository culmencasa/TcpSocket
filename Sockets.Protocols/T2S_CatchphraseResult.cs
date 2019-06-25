using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Protocols
{

    public class T2S_CatchphraseResult : IProtocolInfo
    {
        public T2S_CatchphraseResult()
        {
            msg = new InnerMsg();
        }
        public InnerMsg msg { get; set; }

        public class InnerMsg
        {
            /// <summary>
            /// 抢到的学生id
            /// </summary>
            public string strCatcher { get; set; }

            /// <summary>
            /// 状态: 0 未结束 1 结束
            /// </summary>
            public int status { get; set; }
        }

        public int cmd => (int)ProtocolCmds.T2S_CatchphraseResult;
    }
}
