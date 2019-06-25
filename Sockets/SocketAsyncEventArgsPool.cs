using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Sockets
{
    /// <summary>
    /// ��ʾһ������õ�SocketAsyncEventArgs���󼯺�
    /// ��MSDN: http://msdn2.microsoft.com/zh-cn/library/system.net.sockets.socketasynceventargs.socketasynceventargs.aspx
    /// </summary>
    public sealed class SocketAsyncEventArgsPool 
    {
        /// <summary>
        /// ���ڴ�ſ�����SocketAsyncEventArgs�������ȳ�(LIFO)����
        /// </summary>
        Stack<SocketAsyncEventArgs> pool;

        /// <summary>
        /// ��ʼ��ָ����С�Ķ����.
        /// </summary>
        /// <param name="capacity">���������ֵ</param>
        public SocketAsyncEventArgsPool(Int32 capacity)
        {
            this.pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        /// <summary>
        /// �Ƴ�һ����Ա.
        /// </summary>
        /// <returns>SocketAsyncEventArgʵ��.</returns>
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
        /// ����һ����Ա
        /// </summary>
        /// <param name="item">SocketAsyncEventArgʵ��.</param>
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) 
            { 
                throw new ArgumentNullException("��ӵ����ϵĳ�Ա����Ϊ�ա�"); 
            }

            lock (this.pool)
            {
                this.pool.Push(item);
            }
        }


        /// <summary>
        /// ���ϵĳ�Ա����
        /// </summary>
        public int Count
        {
            get { return this.pool.Count; }
        }


    }
}
