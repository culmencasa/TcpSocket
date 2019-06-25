using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Protocols
{
    public class T2S_SCREENQUESTION : IProtocolInfo
    {
        public int cmd
        {
            get
            {
                return (int)ProtocolCmds.T2S_ScreenQuestion;
            }
        }

        public InnerMsg msg { get; set; }

        public class InnerMsg
        {
            /// <summary>
            /// 截屏Id
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
        }

    }
}
