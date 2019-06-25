using NLog;
using Sockets;
using Sockets.Common;
using Sockets.Default;
using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sockets.Demo
{
    /// <summary>
    /// 服务管理类
    /// </summary>
    public static class ServerManager
    {

        #region 服务的外部事件注册绑定与清理

        static void Bind(TcpServer server, ServerEvents evts)
        {
            if (server == null)
                return;

            foreach (var handler in evts.ConnectedEventList)
            {
                if (handler != null && handler.Target != null)
                {
                    server.ClientConnected += handler;
                }
            }
            foreach (var handler in evts.DisconnectedEventList)
            {
                if (handler != null && handler.Target != null)
                {
                    server.ClientDisconnected += handler;
                }
            }
            foreach (var handler in evts.SentEventList)
            {
                if (handler != null && handler.Target != null)
                {
                    server.MessageSend += handler;
                }
            }
            foreach (var handler in evts.ReceivedEventList)
            {
                if (handler != null && handler.Target != null)
                {
                    server.MessageReceived += handler;
                }
            }
        }

        static void Unbind(TcpServer server, ServerEvents evts)
        {
            if (server == null)
                return;

            foreach (var handler in evts.ConnectedEventList)
            {
                if (handler != null && handler.Target != null)
                {
                    server.ClientConnected -= handler;
                }
            }
            foreach (var handler in evts.DisconnectedEventList)
            {
                if (handler != null && handler.Target != null)
                {
                    server.ClientDisconnected -= handler;
                }
            }
            foreach (var handler in evts.SentEventList)
            {
                if (handler != null && handler.Target != null)
                {
                    server.MessageSend -= handler;
                }
            }
            foreach (var handler in evts.ReceivedEventList)
            {
                if (handler != null && handler.Target != null)
                {
                    server.MessageReceived -= handler;
                }
            }
        }

        static void ClearEventsRegistering()
        {
            Events.ClientConnectedEventChanged -= OnConnectedEventRegisterChanged;
            Events.ClientDisconnectedEventChanged -= OnDisconnectedEventRegisterChanged;
            Events.MessageSendEventChanged -= OnSendEventRegisterChanged;
            Events.MessageReceivedEventChanged -= OnReceivedEventRegisterChanged;

            Events.ConnectedEventList.Clear();
            Events.DisconnectedEventList.Clear();
            Events.SentEventList.Clear();
            Events.ReceivedEventList.Clear();
        }


        static void OnConnectedEventRegisterChanged(object target, EventHandler<ClientStateEventArgs> method)
        {
            if (Events.ConnectedEventList.Contains(method))
            {
                if (TcpServer != null)
                    TcpServer.ClientConnected += method;
            }
            else
            {
                if (TcpServer != null)
                    TcpServer.ClientConnected -= method;
            }

        }


        static void OnDisconnectedEventRegisterChanged(object target, EventHandler<ClientStateEventArgs> method)
        {
            if (Events.DisconnectedEventList.Contains(method))
            {
                if (TcpServer != null)
                    TcpServer.ClientDisconnected += method;
            }
            else
            {
                if (TcpServer != null)
                    TcpServer.ClientDisconnected -= method;
            }
        }


        static void OnSendEventRegisterChanged(object target, EventHandler<ClientStateEventArgs> method)
        {
            if (Events.SentEventList.Contains(method))
            {
                if (TcpServer != null)
                    TcpServer.MessageSend += method;
            }
            else
            {
                if (TcpServer != null)
                    TcpServer.MessageSend -= method;
            }
        }


        static void OnReceivedEventRegisterChanged(object target, EventHandler<ClientStateEventArgs> method)
        {
            if (Events.ReceivedEventList.Contains(method))
            {
                if (TcpServer != null)
                    TcpServer.MessageReceived += method;
            }
            else
            {
                if (TcpServer != null)
                    TcpServer.MessageReceived -= method;
            }
        }

        #endregion


        #region 字段

        private static Logger logger = LogManager.GetCurrentClassLogger();


        #endregion

        #region 属性

        /// <summary>
        /// Tcp服务器
        /// </summary>
        static TcpServer TcpServer { get; set; }

        /// <summary>
        /// Udp组播
        /// </summary>
        static UdpMulticast UdpBeeper { get; set; }


        /// <summary>
        /// 消息包相关设置
        /// </summary>
        static PackageSettings PackageSetting { get; set; }


        /// <summary>
        /// 解析类
        /// </summary>
        public static PackageDealer DefaultPackageDealer { get; } = new PackageDealer();

        /// <summary>
        /// 公开的事件管理
        /// </summary>
        public static ServerEvents Events { get; set; }

        /// <summary>
        /// TCP服务器是否启动
        /// </summary>
        public static bool IsOpened
        {
            get
            {
                return TcpServer == null ? false : TcpServer.IsServerRunning();
            }

        }

        /// <summary>
        /// 公开服务器节点
        /// </summary>
        public static IPEndPoint ServerEndPoint
        {
            get; private set;
        }


        /// <summary>
        /// 学生->设备状态 字典对象
        /// </summary>
        public static ClientDictionary StudentDevices { get; set; }


        #endregion

        #region 构造

        static ServerManager()
        {
            PackageSetting = new PackageSettings();
            PackageSetting.PrefixString = "**_"; //todo: 改为配置
            PackageSetting.LengthString = "4";
            PackageSetting.Encoding = Encoding.UTF8;
            PackageSetting.BufferSize = 102400;
            PackageSetting.MaxConnectionNumber = 120;

            Events = new ServerEvents();

            #region 设置绑定事件

            // 如果程序运行中Tcp服务重启, 调用Bind(Events)方法重新绑定之前绑定过的事件, 待优化...

            Events.ClientConnectedEventChanged += OnConnectedEventRegisterChanged;
            Events.ClientDisconnectedEventChanged += OnDisconnectedEventRegisterChanged;
            Events.MessageSendEventChanged += OnSendEventRegisterChanged;
            Events.MessageReceivedEventChanged += OnReceivedEventRegisterChanged;

            #endregion

            DefaultPackageDealer.PackageSetting = PackageSetting;

        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 开启Tcp服务
        /// </summary>
        public static void StartTcpServer()
        {
            if (TcpServer != null && TcpServer.IsServerRunning())
            {
                TcpServer.Stop();
            }

            TcpServer = new TcpServer(PackageSetting);
            TcpServer.Timeout = 40;
            TcpServer.ClientConnected += TcpServer_ConnectedPreset;
            TcpServer.MessageReceived += TcpServer_MessageReceivedPreset;
            TcpServer.MessageSend += TcpServer_MessageSendPreset;
            TcpServer.ClientDisconnected += TcpServer_DisconnectedPreset;

            TcpServer.CreateClientHandler = (conn, setting) =>
            {
                return new TcpClientProxy(conn, setting);
            };

            Bind(TcpServer, Events);

            var info = Network.GetActiveIPv4();
            IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(info.IpAddress), Convert.ToInt32(9900));
            TcpServer.Start(localEP);


            ServerEndPoint = localEP;

        }

        /// <summary>
        /// 停止Tcp服务
        /// </summary>
        public static void StopTcpServer()
        {
            if (TcpServer != null && TcpServer.IsServerRunning())
            {
                Unbind(TcpServer, Events);
                Events.Clear();

                TcpServer.ClientConnected -= TcpServer_ConnectedPreset;
                TcpServer.ClientDisconnected -= TcpServer_DisconnectedPreset;
                TcpServer.MessageReceived -= TcpServer_MessageReceivedPreset;
                TcpServer.MessageSend -= TcpServer_MessageSendPreset;
                TcpServer.Stop();
            }

        }

        /// <summary>
        /// 手动回收
        /// </summary>
        /// <param name="e"></param>
        public static void Recycle(SocketAsyncEventArgs e)
        {
            TcpServer.RecycleContext(e);
        }

        /// <summary>
        /// 开启组播
        /// </summary>
        /// <param name="info"></param>
        public static void StartUdpBeeper(MulticastInfo info)
        {
            if (UdpBeeper == null)
            {
                UdpMulticast.Settings setting = new UdpMulticast.Settings();
                setting.TargetIP = "230.0.0.0";
                setting.TargetPort = 4321;
                setting.LocalIP = Network.GetActiveIPv4().IpAddress;
                setting.LocalPort = 2094;
                setting.Period = 7000;

                UdpBeeper = new UdpMulticast(setting);
                UdpBeeper.Start(info);
            }
        }

        /// <summary>
        /// 停止组播
        /// </summary>
        public static void StopUdpBeeper()
        {
            if (UdpBeeper != null)
            {
                UdpBeeper.Stop();
                UdpBeeper = null;
            }
        }

        #endregion

        #region 默认事件

        private static void TcpServer_MessageSendPreset(object sender, ClientStateEventArgs e)
        {
            //Console.WriteLine("发送了一条消息");
        }

        private static void TcpServer_MessageReceivedPreset(object sender, ClientStateEventArgs e)
        {
            //HandleReceivedPackages(e, (context, protocol) =>
            //{
            //    Console.WriteLine(protocol.ToString()); 
            //});
        }

        private static void TcpServer_DisconnectedPreset(object sender, ClientStateEventArgs e)
        {
            //Console.WriteLine(string.Format(" **** {0} 客户端{1} 断线了 **** ", DateTime.Now.ToString("HH:mm:ss"), e.ClientEndPoint));
        }

        private static void TcpServer_ConnectedPreset(object sender, ClientStateEventArgs e)
        {
            //Console.WriteLine(string.Format(" **** {0} 客户{1} 连接上了 **** ", DateTime.Now.ToString("HH:mm:ss"), e.ClientEndPoint));
        }

        #endregion


        public static void Send(IProtocolInfo info)
        {
            var messageThread = new Thread(() =>
            {
                try
                {
                    // 遍历所有客户端
                    foreach (var clientPair in StudentDevices)
                    {
                        var studentClient = clientPair.Value;

                        //if (modifyInfoByClient != null)
                        //{
                        //    modifyInfoByClient(clientPair);
                        //}

                        if (studentClient != null)
                        {
                            IPackageInfo package = DefaultPackageDealer.Seal(info);
                            TcpServer.SendAsync(studentClient, package.ToBytes());
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            });
            messageThread.Start();
        }


        public static void Send(string text)
        {
            Task.Run(() =>
            {
                try
                {
                    // 遍历所有客户端
                    foreach (var clientPair in StudentDevices)
                    {
                        var studentClient = clientPair.Value;

                        //if (modifyInfoByClient != null)
                        //{
                        //    modifyInfoByClient(clientPair);
                        //}

                        if (studentClient != null)
                        {
                            byte[] content = Encoding.UTF8.GetBytes(text);
                            TcpServer.SendAsync(studentClient, content);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }

            });
        }


        public static void HandleReceivedPackages(ClientStateEventArgs e, Action<ClientStateEventArgs, IProtocolInfo> handleInnerProtocol)
        {
            string msg = string.Empty;
            if (e.LastPackages != null && e.LastPackages.Count > 0)
            {
                // 遍历接收到的多个封包数据
                foreach (var package in e.LastPackages)
                {
                    // 打开一个封包的内容，如果返回的结果为null，表示协议未知或者未能正常解析
                    IProtocolInfo protocol = ServerManager.DefaultPackageDealer.Open(package);
                    if (protocol != null)
                    {
                        if (handleInnerProtocol != null)
                        {
                            handleInnerProtocol.BeginInvoke(e, protocol, null, null);
                        }
                    }
                    else
                    {
                        logger.Debug("协议未知或者未能正常解析。");
                    }
                }
            }
            else
            {
                msg = e.LastReceivedMessage;

                logger.Debug("非协议消息" + msg);
            }
        }

    }
}
