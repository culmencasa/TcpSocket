using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Protocols
{

    /// <summary>
    /// 锁屏/解锁
    /// </summary>
    public class T2S_LockScreenInfo : IProtocolInfo
    {
        public T2S_LockScreenInfo()
        {
            msg = new InnerMsg();
        }
        public InnerMsg msg { get; set; }

        public class InnerMsg
        {
            /// <summary>
            /// 是否锁屏
            /// 0：解除
            /// 1：锁定
            /// 2: 下课
            /// </summary>
            public int state { get; set; }
        }


        public int cmd => (int)ProtocolCmds.T2S_Lock;
    }
}
