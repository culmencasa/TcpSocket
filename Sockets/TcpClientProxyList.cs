using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.ObjectModel;
using System.Net;
using Sockets.Interfaces;

namespace Sockets
{
    public class TcpClientProxyList : List<ITcpClientProxy>
    {
        private object lockObject = new object();

        /// <summary>
        /// 索引器
        /// </summary>
        /// <param name="remoteIP"></param>
        /// <returns></returns>
        public ITcpClientProxy this[string remoteIP] 
        {
            get
            {
                ITcpClientProxy target = null;
                foreach (var item in this)
                {
                    if (item == null)
                        continue;
                    if (item.Connection == null)
                        continue;
                    if (item.Connection.RemoteEndPoint == null)
                        continue;

                    if (item.Connection.RemoteEndPoint.ToString() == remoteIP)
                    {
                        target = item;
                        break;
                    }
                }

                return target;
            }
        }
        
        /// <summary>
        /// 是否包含指定IP地址（含端口）的客户端
        /// </summary>
        /// <param name="ep"></param>
        /// <returns></returns>
        public bool ContainsKey(EndPoint ep)
        {
            bool contains = false;
            
            string remoteIP = ep.ToString();
            if (this[remoteIP] != null)
            {
                contains = true;
            }

            return contains;
        }

        /// <summary>
        /// 添加一个客户端
        /// </summary>
        /// <param name="newClient"></param>
        public new void Add(ITcpClientProxy newClient)
        {
            // 什么情况会为空值未知
            if (newClient == null || newClient.Connection == null || newClient.Connection.RemoteEndPoint == null)
                return;

            EndPoint key = newClient.Connection.RemoteEndPoint;
            if (!this.ContainsKey(key))
            {
                lock (lockObject)
                {
                    base.Add(newClient);
                }
            }
            else
            {
                lock (lockObject)
                {
                    ITcpClientProxy oldClient = this[key.ToString()];
                    base.Remove(oldClient);
                    base.Add(newClient);
                }
            }
        }

        /// <summary>
        /// 删除一个客户端 
        /// </summary>
        /// <param name="targetClient"></param>
        public new void Remove(ITcpClientProxy targetClient)
        {
            if (targetClient == null)
                return;


            lock (lockObject)
            {
                for (int i = this.Count - 1; i >= 0; i--)
                {
                    var comp = this[i];
                    if (targetClient == comp)
                    {
                        base.Remove(comp);
                        break;
                    }
                }
            }
        }

    }
}
