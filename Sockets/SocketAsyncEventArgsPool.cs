using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Sockets
{
    /// <summary>
    /// 表示一组可重用的SocketAsyncEventArgs对象集合
    /// 见MSDN: http://msdn2.microsoft.com/zh-cn/library/system.net.sockets.socketasynceventargs.socketasynceventargs.aspx
    /// </summary>
    public sealed class SocketAsyncEventArgsPool 
    {
        /// <summary>
        /// 用于存放可重用SocketAsyncEventArgs对象后进先出(LIFO)集合
        /// </summary>
        Stack<SocketAsyncEventArgs> pool;

        /// <summary>
        /// 初始化指定大小的对象池.
        /// </summary>
        /// <param name="capacity">容器的最大值</param>
        public SocketAsyncEventArgsPool(Int32 capacity)
        {
            this.pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        /// <summary>
        /// 移除一个成员.
        /// </summary>
        /// <returns>SocketAsyncEventArg实例.</returns>
        public SocketAsyncEventArgs Pop()
        {
            lock (this.pool)
            {
                if (this.pool.Count > 0)
                {
                    return this.pool.Pop();
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 增加一个成员
        /// </summary>
        /// <param name="item">SocketAsyncEventArg实例.</param>
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) 
            { 
                throw new ArgumentNullException("添加到集合的成员不能为空。"); 
            }

            lock (this.pool)
            {
                this.pool.Push(item);
            }
        }


        /// <summary>
        /// 集合的成员个数
        /// </summary>
        public int Count
        {
            get { return this.pool.Count; }
        }


    }
}
