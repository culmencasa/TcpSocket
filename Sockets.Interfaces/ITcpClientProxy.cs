using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Sockets.Interfaces
{
    /// <summary>
    /// 客户端接口
    /// </summary>
    public interface ITcpClientProxy
    {
        #region 事件

        event Func<ITcpClientProxy, bool> ValidatingHandler;

        #endregion

        #region 属性

        /// <summary>
        /// 与客户端一一对应的业务ID
        /// </summary>
        string BusinessID { get; set; }

        /// <summary>
        /// 连接的Socket对象
        /// </summary>
        Socket Connection { get; }


        /// <summary>
        /// 接收消息的上下文(与Socket关联)
        /// </summary>
        SocketAsyncEventArgs ReceiveContext { get; set; }


        /// <summary>
        /// 上次接收的内容集合(积压的数据最后可能拼出多个IPackageInfo对象)
        /// </summary>
        List<IPackageInfo> LastPackages { get; }

        /// <summary>
        /// 上次接收的消息文本
        /// </summary>
        string LastReceivedString { get;  }
        
        /// <summary>
        /// 上次接收的消息字节集合
        /// </summary>
        byte[] LastReceivedBytes { get; }

        /// <summary>
        /// 客户端状态
        /// </summary>
        int ClientStatus { get; set;  }

        /// <summary>
        /// 反馈时间
        /// </summary>
        DateTime FeedbackTime { get; set; }

        /// <summary>
        /// 用于交换临时数据
        /// </summary>
        object ExchangedData { get; set; }

        /// <summary>
        /// 是否已注销
        /// </summary>
        bool IsDisposed { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 从通讯上下文中接收信息封包
        /// 不会抛出异常, 见日志.
        /// </summary>
        /// <param name="receiveContext"></param>
        /// <param name="packages"></param>
        /// <returns></returns>
        bool TryReceivePackages(SocketAsyncEventArgs receiveContext, out List<IPackageInfo> packages);

        /// <summary>
        /// 为Socket上下文设置发送缓冲区
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sendBuffer"></param>
        void PrepareOutgoingData(SocketAsyncEventArgs context, byte[] sendBuffer);


        /// <summary>
        /// 注销工作
        /// </summary>
        void Dispose();

        #endregion
    }
}
