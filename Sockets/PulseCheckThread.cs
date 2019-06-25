using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sockets
{
    /// <summary>
    /// 心跳检查线程
    /// </summary>
    internal class PulseCheckThread 
    {
        #region 字段

        Thread _innerThread;


        ITcpServer _server;


        #endregion

        #region 构造

        public PulseCheckThread(ITcpServer server)
        {
            _server = server;
        }

        #endregion

        #region 属性
        
        /// <summary>
        /// 线程是否正忙
        /// </summary>
        public bool IsBusy
        {
            get
            {
                if (_innerThread == null)
                    return false;

                return _innerThread.IsAlive;
            }
        }

        /// <summary>
        /// 时长(默认10秒)
        /// </summary>
        public int Interval { get; set; } = 10000;

        /// <summary>
        /// 过期客户端清除器
        /// </summary>
        public Action<IList<ITcpClientProxy>> EliminationHandler
        {
            get;
            set;
        }
        
        #endregion

        #region 公开方法

        /// <summary>
        ///  尝试启动线程，如果线程正在活动中，则直接返回。
        /// </summary>
        public void TryRun()
        {
            if (this.IsBusy)
            {
                Console.WriteLine("心跳检查线程已在运行中。");
                return;
            }

            _innerThread = new Thread(CheckEachClientStates);
            _innerThread.IsBackground = true;
            _innerThread.Start(_server);
        }

        /// <summary>
        /// 结束线程
        /// </summary>
        public void Close()
        {
            _innerThread.Abort();
            _innerThread.Join();
        }

        #endregion

        #region 私有方法
        
        private void CheckEachClientStates(object argument)
        {
            TcpServer targetServer = argument as TcpServer;
            if (targetServer == null)
                return;

            int timeout = targetServer.Timeout;

            try
            {
                do
                {
                    if (targetServer == null || !targetServer.IsServerRunning())
                        return;

                    IList<ITcpClientProxy> clientList = targetServer.GetClientCollections();
                    IList<ITcpClientProxy> expiredList = new List<ITcpClientProxy>();
                    if (clientList.Count > 0)
                    {

                        foreach (var member in clientList)
                        {
                            if (member.ClientStatus != (int)ClientStateEnums.Connected)
                                continue;

                            DateTime lastTickCount = member.FeedbackTime;
                            DateTime thisTickCount = DateTime.Now;
                            int timespan = (thisTickCount - lastTickCount).Seconds;

                            // test
                            Console.WriteLine("span:" + timespan.ToString());

                            if (timespan > timeout)
                            {
                                expiredList.Add(member);
                            }
                        }

                        if (expiredList.Count > 0 && EliminationHandler != null)
                        {
                            EliminationHandler(expiredList);
                        }
                        

                        //for (int index = expiredList.Count - 1; index >= 0; index--)
                        //{
                        //    if (!_innerThread.IsAlive)
                        //        break;

                        //    var member = expiredList[index];

                        //    //todo: 改为服务方法
                        //    member.Dispose();
                        //    member.ClientStatus = ClientStates.Closed;
                        //    //tlist.Remove(member);
                        //    //member.Dispose();
                        //}
                    }

                    Thread.Sleep(Interval);

                } while (_innerThread.IsAlive);
            }
            catch (InvalidOperationException invalidOperationExeption)
            {
                // 什么情况下会发生未明

                Console.WriteLine(invalidOperationExeption.Message);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

    }
}
