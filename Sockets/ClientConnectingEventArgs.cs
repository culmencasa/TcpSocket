using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sockets
{
    
    public class ClientConnectingEventArgs : EventArgs
    {
        /// <summary>
        /// 获取或设置处理连接客户端的验证方法
        /// </summary>
        public Func<IPackageInfo, bool> ConnectionValidationHandler { get; set; }

    }
}
