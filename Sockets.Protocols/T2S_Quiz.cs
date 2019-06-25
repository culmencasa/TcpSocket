using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Protocols
{
    public class T2S_Quiz : IProtocolInfo
    {
        public InnerMsg msg { get; set; } = new InnerMsg();

        public int cmd => (int)ProtocolCmds.T2S_Quiz;

        public class InnerMsg
        {
            /// <summary>
            /// 题目Id
            /// </summary>
            public string busId { get; set; }
            /// <summary>
            /// 图片链接
            /// </summary>
            public string source { get; set; }

            /// <summary>
            /// 是否需要答题。1-需要，0-不需要
            /// </summary>
            public string needReply { get; set; }

            /// <summary>
            /// 截屏答题类型。
            /// 0-全部答题，1-小组答题、2-点名答题、3-随机答题、4抢答答题
            /// </summary>
            public string type { get; set; }

            /// <summary>
            ///  a(手写), b(笔写)
            /// 注:配置文件里notable=0时才发这个,即有平板交互；notable=1时，没有socket交互
            /// </summary>
            public string zz { get; set; } = "b";
        }

    }
}
