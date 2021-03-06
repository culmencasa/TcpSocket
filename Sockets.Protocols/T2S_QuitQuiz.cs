﻿using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Protocols
{
    public class T2S_QuitQuiz : IProtocolInfo
    {
        public InnerMsg msg { get; set; }

        public int cmd => (int)ProtocolCmds.T2S_QuitQuiz;

        public class InnerMsg
        {
            /// <summary>
            /// 题目Id
            /// </summary>
            public string busId { get; set; }

            /// <summary>
            /// 截屏答题类型。
            /// 0-全部答题，1-小组答题、2-点名答题、3-随机答题、4抢答答题
            /// </summary>
            public string type { get; set; }

        }

    }
}
