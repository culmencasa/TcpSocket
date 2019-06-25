using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Net;
using Sockets.Interfaces;

namespace Sockets
{
    /// <summary>
    /// 自定义的Socket事件传递参数
    /// </summary>
    public class ClientStateEventArgs : EventArgs
    {
        #region 构造

        public ClientStateEventArgs(SocketAsyncEventArgs e) 
        {
            this.ClientProxy = e.UserToken as ITcpClientProxy;
            this.ServerEndPoint = this.ClientProxy.Connection.LocalEndPoint;
            this.ClientEndPoint = this.ClientProxy.Connection.RemoteEndPoint;
            this.LastPackages = this.ClientProxy.LastPackages;
            this.LastReceivedMessage = this.ClientProxy.LastReceivedString;
            this.ClientSocketContext = e;
            //this.BusinessId = this.ClientProxy.BusinessID;
            //this.LastOperation = e.LastOperation;
            //this.LastOperationError = e.SocketError;
            //this.LastReceivedMessage = this.ClientToken.LastMessage;
        }

        #endregion


        /// <summary>
        /// 客户端Socket上下文(原始连接对象)
        /// </summary>
        public SocketAsyncEventArgs ClientSocketContext { get; private set; }
        
        /// <summary>
        /// 客户端代理(自定义对象)
        /// </summary>
        public ITcpClientProxy ClientProxy { get; private set; }

        /// <summary>
        /// 客户端网络标识
        /// </summary>
        public EndPoint ClientEndPoint { get; internal set; }


        /// <summary>
        /// 服务端网络标识
        /// </summary>
        public EndPoint ServerEndPoint { get; internal set; }


        /// <summary>
        /// 当前客户端状态
        /// </summary>
        public ClientStateEnums ClientStatus { get; internal set; }

        public List<IPackageInfo> LastPackages { get; internal set; }

        public string LastReceivedMessage { get; internal set; }

        //public object BusinessId { get; private set; }

        //public SocketError LastOperationError { get; internal set; }

        //public SocketAsyncOperation LastOperation { get; internal set; }

        //public MessageInfo LastSendMessage { get; internal set; }

        /// <summary>
        /// 获取或设置响应消息
        /// </summary>
        public IPackageInfo ResponseMessage { get; private set; }

    }

}

