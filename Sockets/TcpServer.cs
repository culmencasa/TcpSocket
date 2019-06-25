using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Sockets
{
    /// <summary>
    /// Tcp服务
    /// 参考MSDN源例: http://msdn2.microsoft.com/zh-cn/library/system.net.sockets.socketasynceventargs.aspx
    /// </summary>
    public class TcpServer : ITcpServer
    {
        #region 委托和事件

        public event EventHandler<ClientStateEventArgs> ClientConnected;
        public event EventHandler<ClientStateEventArgs> ClientDisconnected;
        
        public event EventHandler<ClientStateEventArgs> MessageSend;
        public event EventHandler<ClientStateEventArgs> MessageReceived;

        public Func<Socket, IPackageSettings, ITcpClientProxy> CreateClientHandler;



        #endregion

        #region 静态成员

        /// <summary>
        /// 读，写（不为接收分配缓冲）
        /// </summary>
        const int opsToPreAlloc = 2;     

        /// <summary>
        /// 用于服务器执行的互斥同步对象.
        /// </summary>
        protected static Mutex mutex = new Mutex();

        #endregion

        #region 字段

        /// <summary>
        /// tcp服务的终结点
        /// </summary>
        private IPEndPoint _serverEndPoint;

        /// <summary>
        /// 日志对象
        /// </summary>
        protected EZLogger _logger;

        /// <summary>
        /// 获用于发送/接收消息的编码格式
        /// </summary>
        protected Encoding _bufferEncoding;


        /// <summary>
        /// 用于监听连接请求的Socket.
        /// </summary>
        protected Socket _socketListener;

        /// <summary>
        /// 用于每个I/O Socket操作的缓冲区大小.
        /// </summary>
        protected int _receiveBufferSize;

        /// <summary>
        /// 挂起连接队列的最大长度
        /// </summary>
        protected int _backlog;

        /// <summary>
        /// 表示一套用于所有套接字操作的可重用缓存
        /// </summary>
        private static BufferManager _bufferManager;


        /// <summary>
        /// 用于接收连接的SocketAsyncEventArgs对象池
        /// </summary>
        protected SocketAsyncEventArgsPool _poolOfAcceptEventArgs;

        /// <summary>
        /// 用于接收收发消息的SocketAsyncEventArgs对象池
        /// </summary>
        protected SocketAsyncEventArgsPool _poolOfReadWriteEventArgs;


        /// <summary>
        /// 控制连接到服务器的客户总数.
        /// </summary>
        protected Semaphore _semaphoreAcceptedClients;

        /// <summary>
        /// 客户端集合
        /// </summary>
        protected TcpClientProxyList _clientMembers; //todo: 改为外部可扩展的对象

        /// <summary>
        /// 心跳线程
        /// </summary>
        private PulseCheckThread _pulseWorkThread;

        /// <summary>
        /// 用于锁
        /// </summary>
        private object _tcpLock = new object();

        #endregion

        #region 属性

        /// <summary>
        /// 服务器上的已连接数
        /// </summary>
        public int ConnectedCount
        {
            get
            {
                return _clientMembers.Count;
            }
        }

        /// <summary>
        /// 是否回传客户端消息(仅针对非协议消息，例如心跳)
        /// </summary>
        public bool Echo
        {
            get;
            set;
        }

        /// <summary>
        /// 获取或设置客户超时的秒数
        /// </summary>
        public int Timeout
        {
            get;
            set;
        }

        /// <summary>
        /// 客户处理类集合
        /// </summary>
        public IList<ITcpClientProxy> GetClientCollections()
        {
            return _clientMembers.AsReadOnly();
        }

        /// <summary>
        /// 服务器终结点
        /// </summary>
        public IPEndPoint ServerEndPoint
        {
            get
            {
                return _serverEndPoint;
            }
        }


        public IPackageSettings Setting { get; set; }

        #endregion

        #region 构造

        /// <summary>
        /// 实例化一个面向连接的侦听服务器, 并指定编码格式，最大连接数和读写缓冲大小
        /// </summary>
        /// <param name="maxConnectionNumber">服务器同时处理的最大连接数量.</param>
        /// <param name="receiveBufferSize">每个I/O操作的缓冲大小.</param>
        public TcpServer(IPackageSettings setting)
        {
            // 启动日志
            StartLog();

            _bufferEncoding = setting.Encoding;
            _backlog = setting.MaxConnectionNumber;
            _receiveBufferSize = setting.BufferSize;

            _semaphoreAcceptedClients = new Semaphore(_backlog, _backlog);
            _poolOfAcceptEventArgs = new SocketAsyncEventArgsPool(_backlog);
            _poolOfReadWriteEventArgs = new SocketAsyncEventArgsPool(_backlog);
            //_sendPool = new SocketAsyncEventArgsPool(this._backlog);

            this.Setting = setting;

            // 设置心跳超时
            Timeout = 30; // 默认30秒

            _clientMembers = new TcpClientProxyList();

            InitializeContexts();
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 启动服务，侦听连接请求.
        /// </summary>
        public void Start(IPEndPoint localEndPoint)
        {
            try
            {
                // 1.创建侦听对象
                _socketListener = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _socketListener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _socketListener.ReceiveBufferSize = this._receiveBufferSize;
                _socketListener.SendBufferSize = this._receiveBufferSize;

                // 2.绑定IPv4或IPv6
                if (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    _serverEndPoint = new IPEndPoint(IPAddress.IPv6Any, localEndPoint.Port);

                    // 27 在winsock里表示 IPV6_V6ONLY 选项, 详见
                    // http://blogs.msdn.com/wndp/archive/2006/10/24/creating-ip-agnostic-applications-part-2-dual-mode-sockets.aspx
                    _socketListener.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                    _socketListener.Bind(_serverEndPoint);


                }
                else
                {
                    _serverEndPoint = localEndPoint;
                    _socketListener.Bind(localEndPoint);
                }

                // 3.尝试侦听传入的TCP连接。 backlog 参数指定队列中最多可容纳的等待接受的传入连接数。 
                _socketListener.Listen(_backlog);

                this.StartAccept();

                this.StartCheckingPulse();

                // 阻塞当前线程，接收传入的消息. 
                mutex.WaitOne();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.StackTrace);
                throw new Exception("启动Tcp服务失败。");
            }
        }

        /// <summary>
        /// 停止侦听服务.
        /// </summary>
        public void Stop()
        {
            _logger.Info("主动关闭TCP, 断开接连。");


            // 先通知所有客户端断开
            if (_clientMembers != null)
            {
                for (int index = _clientMembers.Count - 1; index >= 0; index--)
                {
                    var clientHandler = _clientMembers[index];

                    try
                    {
                        if (this.ClientDisconnected != null)
                        {
                            ClientStateEventArgs stateObject = new ClientStateEventArgs(clientHandler.ReceiveContext);
                            stateObject.ClientStatus = ClientStateEnums.Closed;

                            this.ClientDisconnected(this, stateObject);
                        }

                        //_clientHandlers.Remove(clientHandler);

                    }
                    catch (Exception ex)
                    {
                        // 外部未处理的异常，记录日志，不影响主流程。
                        _logger.Error(ex.StackTrace);
                    }

                    //this.RecycleContext(clientHandler.ReceiveContext);
                }
            }

            lock (_tcpLock)
            {
                // 1.pulse thread close
                this.StopCheckingPulse();

                // 清理缓存，事件
                SocketAsyncEventArgs eventArgs;
                while (this._poolOfAcceptEventArgs.Count > 0)
                {
                    eventArgs = _poolOfAcceptEventArgs.Pop();
                    eventArgs.Completed -= SocketAsyncEventArgs_IOCompleted;
                    //_bufferManager.FreeBuffer(eventArgs);
                    eventArgs.Dispose();
                }
                while (this._poolOfReadWriteEventArgs.Count > 0)
                {
                    eventArgs = _poolOfReadWriteEventArgs.Pop();
                    eventArgs.Completed -= SocketAsyncEventArgs_IOCompleted;
                    _bufferManager.FreeBuffer(eventArgs);
                    eventArgs.Dispose();
                }


                _bufferManager = null;
                //GC.Collect();

                try
                {
                    //_socketListener.Disconnect(true);
                    //_socketListener.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.StackTrace);
                }
                finally
                {
                    if (_socketListener != null)
                    {
                        _socketListener.Close();
                        _socketListener.Dispose();
                        _socketListener = null;
                    }
                }

                mutex.ReleaseMutex();
                this._semaphoreAcceptedClients.Release();


                // 清掉当前客户端
                foreach (var client in _clientMembers)
                {
                    Socket s = client.Connection;
                    try
                    {
                        s.Close();
                        //s.Shutdown(SocketShutdown.Receive);
                        //s.Disconnect(true);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        if (s.Connected)
                        {
                            s.Close();
                            s.Dispose();
                        }
                        s = null;
                    }
                }
                _clientMembers.Clear();
            }
        }

        /// <summary>
        /// 服务是否启动中 (改成枚举)
        /// </summary>
        /// <returns></returns>
        public bool IsServerRunning()
        {
            if (_socketListener == null)
                return false;

            return true;
        }

        /// <summary>
        /// 同步发送消息给客户端
        /// </summary>
        /// <param name="target"></param>
        /// <param name="infomation"></param>
        public bool Send(ITcpClientProxy target, byte[] sendBuffer)
        {
            SocketAsyncEventArgs context = target.ReceiveContext;

            if (!IsSocketAlive(context))
                return false;

            try
            {
                context.AcceptSocket.Send(sendBuffer);
                return true;
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10058)
                {
                    _logger.Error("客户端已断开。");
                }

                this.RecycleContext(context);
                return false;
            }
            finally
            { 
                
            }
        }


        public bool Send(ITcpClientProxy target, IPackageInfo content)
        {
            return Send(target, content.ToBytes());
        }



        /// <summary>
        /// 异步发送消息给客户端
        /// </summary>
        /// <param name="target"></param>
        /// <param name="sendBuffer"></param>
        public void SendAsync(ITcpClientProxy target, byte[] sendBuffer)
        {
            //Byte[] sendBuffer = this._bufferEncoding.GetBytes(infomation);

            byte[] cacheBuffer = new byte[_receiveBufferSize];
            Buffer.BlockCopy(sendBuffer, 0, cacheBuffer, 0, sendBuffer.Length);
               
            // unused 
            //SocketAsyncEventArgs context = this._sendPool.Pop();
            
            SocketAsyncEventArgs context = _poolOfReadWriteEventArgs.Pop();
            if (context == null)
            {
                context = new SocketAsyncEventArgs();
            }
            context.AcceptSocket = target.Connection;
            context.SetBuffer(cacheBuffer, 0, cacheBuffer.Length);
            context.UserToken = target;

            EventHandler<SocketAsyncEventArgs> processAsyncSend = null;
            processAsyncSend =new EventHandler<SocketAsyncEventArgs>(
                (sender, e) => {

                    try
                    {
                        #region 异步发送完毕处理

                        switch (e.LastOperation)
                        {
                            case SocketAsyncOperation.Send:
                                if (context.SocketError == SocketError.Success)
                                {
                                    // 调用发送事件
                                    this.OnMessageSend(context);
                                }
                                else
                                {
                                    // 发送中出错，回收
                                    context.Completed -= processAsyncSend;
                                    this.ProcessError(context);
                                    return;
                                }
                                break;
                        }

                        #endregion

                        // 如果已异步发送完毕，则回收
                        context.Completed -= processAsyncSend;
                        context.AcceptSocket = null;
                        _poolOfReadWriteEventArgs.Push(context);
                    }
                    catch (Exception)
                    { 
                        
                    }
            });

            context.Completed += processAsyncSend;

            //if (!IsSocketAlive(context))
            //{
            //    ProcessError(context);
            //    return;
            //} 

            (target as ITcpClientProxy).PrepareOutgoingData(context, sendBuffer);
            if (!IsSocketAlive(context))
                return;

            // 发送回客户端
            if (!context.AcceptSocket.SendAsync(context))
            {
                #region 同步发送完毕

                processAsyncSend(this, context);

                #endregion
            }

        }


        /// <summary>
        /// 重用上次接收操作完毕的上下文对象来发送消息
        /// </summary>
        /// <param name="target"></param>
        /// <param name="sendBuffer"></param>
        public void SendReuseAsync(ITcpClientProxy target, byte[] sendBuffer)
        {
            byte[] cacheBuffer = new byte[_receiveBufferSize];
            Buffer.BlockCopy(sendBuffer, 0, cacheBuffer, 0, sendBuffer.Length);
            SocketAsyncEventArgs context = target.ReceiveContext;

            (target as ITcpClientProxy).PrepareOutgoingData(context, sendBuffer);
            StartSend(context);
        }

        #endregion

        #region 辅助方法

        #region 准备工作

        /// <summary>
        /// 启动日志
        /// </summary>
        protected void StartLog()
        {
            // 启动日志
            _logger = new EZLogger(
                Path.Combine(Environment.CurrentDirectory, "Logs\\TcpServerLog_" + DateTime.Now.ToString("yyyyMMdd") + ".log"),
                true,
                (uint)EZLogger.Level.All);
            _logger.Start();

        }

        /// <summary>
        /// 预分配可重用的缓冲区以及上下文对象，以便提高服务器性能。
        /// </summary>
        protected virtual void InitializeContexts()
        {
            if (_bufferManager == null)
            {
                // 分配缓冲以显著提升大数量的套接字的读写  
                _bufferManager = new BufferManager(
                    _receiveBufferSize * _backlog * opsToPreAlloc, 
                    //this._receiveBufferSize * this._backlog,
                    _receiveBufferSize);
            }
            _bufferManager.InitBuffer();

            // 用于接收连接的SAEA对象不需要设定缓冲区
            for (Int32 i = 0; i < this._backlog; i++)
            { 
                SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
                // 放在Pop时添加，回收时删除事件
                //acceptEventArg.Completed += this.SocketAsyncEventArgs_IOCompleted;

                this._poolOfAcceptEventArgs.Push(acceptEventArg);
            }           

            // 预分配SocketAsyncEventArgs对象池，并关联事件处理
            for (Int32 i = 0; i < this._backlog * opsToPreAlloc; i++)
            {
                SocketAsyncEventArgs readWriteEventArg = new SocketAsyncEventArgs();
                // 放在Pop时添加，回收时删除事件
                //readWriteEventArg.Completed += this.SocketAsyncEventArgs_IOCompleted;

                _bufferManager.SetBuffer(readWriteEventArg);

                this._poolOfReadWriteEventArgs.Push(readWriteEventArg);
            }


            // 暂时不用，因为异步发送时用完就丢掉
            //for (Int32 i = 0; i < this._backlog; i++)
            //{
            //    SocketAsyncEventArgs sendEventArg = new SocketAsyncEventArgs();
            //    sendEventArg.Completed += this.SocketAsyncEventArgs_IOCompleted;
            //    _bufferManager.SetBuffer(sendEventArg);

            //    this._sendPool.Push(sendEventArg);
            //}
        }

        /// <summary>
        /// 开始接收心跳
        /// </summary>
        private void StartCheckingPulse()
        {
            //if (_pulseWorkThread == null)
            //{
            //    _pulseWorkThread = new PulseCheckThread(this);
            //    _pulseWorkThread.EliminationHandler = HandleExpiredClients;
            //}

            //_pulseWorkThread.TryRun();
        }

        /// <summary>
        ///  处理过期的客户端
        /// </summary>
        /// <param name="expiredClientList"></param>
        private void HandleExpiredClients(IList<ITcpClientProxy> expiredClientList)
        {
            if (expiredClientList != null)
            {
                for (int index = expiredClientList.Count - 1; index >= 0; index--)
                {
                    var clientHandler = expiredClientList[index];

                    _logger.Error(string.Format("BusinessId为{0}的学生机心跳超时，将断开接连。", clientHandler.BusinessID));

                    // 通知外部事件
                    try
                    {
                        if (this.ClientDisconnected != null)
                        {
                            ClientStateEventArgs stateObject = new ClientStateEventArgs(clientHandler.ReceiveContext);
                            stateObject.ClientStatus = ClientStateEnums.Closed;

                            this.ClientDisconnected(this, stateObject);
                        }
                    }
                    catch (Exception ex)
                    { 
                        // 外部未处理的异常，记录日志，不影响主流程。
                        _logger.Error(ex.StackTrace);
                    }

                    this.RecycleContext(clientHandler.ReceiveContext); 
                }
            }
        }


        #endregion

        #region SocketAsyncEventArgs相关

        /// <summary>
        /// 判断Sockte是否活动
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected bool IsSocketAlive(SocketAsyncEventArgs e)
        {
            if (e.AcceptSocket == null || !e.AcceptSocket.Connected)
                return false;

            return true;
        }

        /// <summary>
        /// 启动侦听客户端连接操作. 
        /// </summary>
        protected void StartAccept()
        {
            if (!IsServerRunning())
                return;

            SocketAsyncEventArgs acceptEventArg = _poolOfAcceptEventArgs.Pop();
            acceptEventArg.Completed -= this.SocketAsyncEventArgs_AcceptAsyncCompleted;
            acceptEventArg.Completed += this.SocketAsyncEventArgs_AcceptAsyncCompleted;


            this._semaphoreAcceptedClients.WaitOne();

            try
            {
                if (!this._socketListener.AcceptAsync(acceptEventArg))
                {
                    // 重复调用是通过ProcessAccept()方法中对StartAccept->ProcessAccept->StartAccept的递归实现的

                    this.ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// 启动侦听客户端连接操作. 
        /// </summary>
        /// <param name="reuseContext">要用于侦听连接的重用上下文对象</param>
        protected void StartAccept(SocketAsyncEventArgs reuseContext)
        {
            if (!IsServerRunning())
                return;

            // 1.重用SocketAsyncEventArgs对象
            // 在调用 Socket.AcceptAsync 方法之前未提供Socket（设置为 null），.NET将自动创建一个新的Socket
            // 因此重用SocketAsyncEventArgs对象，需要清掉Socket
            reuseContext.AcceptSocket = null; 

            // 2.用于控制连接的个数
            this._semaphoreAcceptedClients.WaitOne();

            // 3.开始异步侦听连接(Accept)请求。
            // AcceptAsync()如果返回false表示操作是同步完成的，不会引发SocketAsyncEventArgs.Completed事件
            if (!this._socketListener.AcceptAsync(reuseContext))
            {
                this.ProcessAccept(reuseContext);
            }
        }

        /// <summary>
        /// 启动接收数据操作
        /// </summary>
        /// <param name="readContext"></param>
        protected void StartReceive(SocketAsyncEventArgs readContext)
        {
            if (!IsSocketAlive(readContext))
                return;

            // 设置要接收的位置和大小(每次都设置，以防止其他地方修改了SocketAsyncEventArgs在全局Buffer里的位置）
            readContext.SetBuffer(readContext.Offset, readContext.Buffer.Length - readContext.Offset);     
            bool willRaiseEvent = readContext.AcceptSocket.ReceiveAsync(readContext);
            if (!willRaiseEvent)
            {
                this.ProcessReceive(readContext);
            }
        }

        /// <summary>
        /// 发送数据操作
        /// </summary>
        /// <param name="e"></param>
        private void StartSend(SocketAsyncEventArgs e)
        {
            if (!IsSocketAlive(e))
                return;

            // 发送回客户端
            if (!e.AcceptSocket.SendAsync(e))
            {
                this.ProcessSend(e);
            }
        }


        /// <summary>
        /// 处理连接请求
        /// </summary>
        /// <param name="acceptContext">与完成Accept操作关联的SocketAsyncEventArg对象</param>
        private void ProcessAccept(SocketAsyncEventArgs acceptContext)
        {
            // 1.新的连接
            Socket acceptSocket = acceptContext.AcceptSocket;
            if (acceptSocket != null && acceptContext.SocketError == SocketError.Success)
            {
                try
                {
                    // 从对象池中取出一个用于读取的SocketAsyncEventArgs对象，将连接到的Socket客户端放入UserToken
                    SocketAsyncEventArgs readEventArgs = _poolOfReadWriteEventArgs.Pop();
                    if (readEventArgs == null)
                    {
                        Trace.WriteLine(string.Format("连接数已满，拒绝 {0} 的连接请求。", acceptSocket.RemoteEndPoint));
                        acceptSocket.Close();
                        return;
                    }

                    readEventArgs.Completed += SocketAsyncEventArgs_IOCompleted;


                    ITcpClientProxy clientHandler = null;

                    #region 处理连接信息

                    if (CreateClientHandler != null)
                    {
                        clientHandler = CreateClientHandler(acceptSocket, Setting);
                        clientHandler.FeedbackTime = DateTime.Now;
                        clientHandler.ReceiveContext = readEventArgs;
                        clientHandler.ClientStatus = (int)ClientStateEnums.Connected;
                    }

                    #endregion

                    //将连接SAEA侦听到的Socket传递给用于收发的SAEA
                    readEventArgs.AcceptSocket = acceptSocket;
                    readEventArgs.UserToken = clientHandler;

                    // 增加连接数
                    if (clientHandler != null)
                    {
                        _clientMembers.Add(clientHandler);
                    }

                    _logger.Info(string.Format("{0} {1}连接上教师机, 目前服务器上有{2}个连接。",
                        DateTime.Now.ToString("HH:mm:ss"),
                        acceptSocket.RemoteEndPoint,
                        this.ConnectedCount));
                        
                    // 通知外部，有新的连接
                    OnClientConnected(readEventArgs);

                    // 开始侦听该连接传入的数据
                    this.StartReceive(readEventArgs);
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine("连接时发生错误：" + ex.StackTrace);
                    _logger.Error("接收连接时错误：" + ex.StackTrace);
                }
            }
            else
            {
                 //Close方法关闭并释放所有资源(托管和非托管), 内部调用了Dispose
                if (acceptSocket != null)
                {
                    acceptSocket.Close();
                }
                _semaphoreAcceptedClients.Release();
            }


            #region 最后的处理，接收下个连接

            // 开始接收新的连接（而不是立即重用当前的acceptContext，以提升性能）
            this.StartAccept();

            // 将SAEA对象放回连接池，以便重用
            acceptContext.Completed -= this.SocketAsyncEventArgs_IOCompleted;
            acceptContext.AcceptSocket = null; // 2015-05-20
            _poolOfAcceptEventArgs.Push(acceptContext);

            #endregion

        }
        
        /// <summary>
        /// 处理接收操作
        /// 当一个异步接收操作完成时调用此方法
        /// </summary>
        /// <param name="receiveContext">与完成Receive操作关联的SocketAsyncEventArg对象</param>
        private void ProcessReceive(SocketAsyncEventArgs receiveContext)
        {

            if (!IsSocketAlive(receiveContext))
                return;

            // BytesTransferred属性提供在可接收或发送数据的异步套接字操作传输的字节数。
            // 如果从读取操作返回零，则说明远程端已关闭了连接
            if (receiveContext.BytesTransferred > 0)
            {
                if (receiveContext.SocketError == SocketError.Success)
                {
                    try
                    {
                        ITcpClientProxy dataHandler = receiveContext.UserToken as ITcpClientProxy;

                        //bool receiveComplete = dataHandler.ReceiveData(receiveContext);
                        List<IPackageInfo> packages = null;
                        bool receiveComplete = dataHandler.TryReceivePackages(receiveContext, out packages);
                        if (receiveComplete)
                        {
                            // 调用消息接收完毕事件
                            try
                            {
                                this.OnMessageReceived(receiveContext);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex.StackTrace);
                            }

                            // 返回响应消息
                            // review: 这里应该用TryReceivePackages返回的集合?
                            IPackageInfo response = dataHandler.ExchangedData as IPackageInfo;
                            if (response != null)
                            {
                                SendReuseAsync(dataHandler, response.ToBytes());
                            }
                           


                            else if (this.Echo)
                            {
                                SendReuseAsync(dataHandler, dataHandler.LastReceivedBytes);
                            }
                            else
                            {
                                StartReceive(receiveContext);
                            }

                            //string exchnagedData = dataHandler.ExchangedData == null ? "" : dataHandler.ExchangedData.ToString();
                            ////todo: 消息分段发送
                            //if (string.IsNullOrEmpty(exchnagedData) && this.Echo)
                            //{
                            //    //dataHandler.Echo(receiveContext);
                            //    //Send(dataHandler, dataHandler.LastMessage.Content);

                            //    if (packages.Count > 0)
                            //    {
                            //        Send(dataHandler, packages[0].ToBytes());
                            //    }

                            //    //Send(dataHandler, packages[0].ToBytes());
                            //}
                            //else
                            //{
                            //    //Send(dataHandler, exchnagedData);
                            //}

                            //dataHandler.PrepareOutgoingData(dataHandler.SendContext, exchnagedData);
                            //StartSend(dataHandler.SendContext);


                        }
                        else
                        {
                            // 如果没有读取完，继续接收下一段数据
                            StartReceive(receiveContext);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Unexpected error:" + ex.StackTrace);
                        Console.WriteLine("Unexpected error:" + ex.StackTrace);
                    }
                }
                else
                {
                    Console.WriteLine("Socket接受时错误, 代号:" + receiveContext.SocketError);
                    _logger.Error("Socket接受时错误, 代号:" + receiveContext.SocketError);

                    this.ProcessError(receiveContext);
                }
                

            }
            else
            {
                // 如果远程端关闭了连接，这里也关闭Socket
                Console.WriteLine("平板端关闭了连接.");
                _logger.Error("平板端关闭了连接.");
                this.ProcessError(receiveContext);
            }
        }

        /// <summary>
        /// 处理发送操作
        /// 当一个发送操作完成时调用此方法  
        /// </summary>
        /// <param name="context">与完成Send操作关联的SocketAsyncEventArg对象.</param>
        private void ProcessSend(SocketAsyncEventArgs context)
        {
            if (context.SocketError == SocketError.Success)
            {
                ITcpClientProxy token = context.UserToken as ITcpClientProxy;
                
                //todo: 长消息分段发
                //receiveSendToken.sendBytesRemainingCount = token.sendBytesRemainingCount - receiveSendEventArgs.BytesTransferred;


                // MSDN的例子
                //if (receiveSendToken.sendBytesRemainingCount == 0)
                //{
                //    // If we are within this if-statement, then all the bytes in
                //    // the message have been sent. 
                //    StartReceive(receiveSendEventArgs);
                //}
                //else
                //{
                //    // If some of the bytes in the message have NOT been sent,
                //    // then we will need to post another send operation, after we store
                //    // a count of how many bytes that we sent in this send op.                    
                //    receiveSendToken.bytesSentAlreadyCount += receiveSendEventArgs.BytesTransferred;
                //    // So let's loop back to StartSend().
                //    StartSend(receiveSendEventArgs);
                //}


                // 调用发送事件
                this.OnMessageSend(context);


                // 读取客户端发送过来的下一段消息.
                StartReceive(context);
            }
            else
            {
                Console.WriteLine("Socket发送时错误，代号:" + context.SocketError);
                _logger.Error("Socket发送时错误，代号:" + context.SocketError);
                this.ProcessError(context);
            }
        }
        
        /// <summary>
        /// 处理错误操作
        /// </summary>
        /// <param name="e"></param>
        private void ProcessError(SocketAsyncEventArgs e)
        {
            ITcpClientProxy handler = e.UserToken as ITcpClientProxy;
            if (!handler.IsDisposed)
            {
                lock (handler)
                {
                    if (handler.IsDisposed)
                        return;
                    
                    Console.WriteLine("{0} 出错了，回收之。{1} : {2}", 
                        e.RemoteEndPoint,
                         e.LastOperation, 
                         e.SocketError
                       );
                    _logger.Error(string.Format("回收了一个客户端{0}。{1} : {2}", 
                        e.RemoteEndPoint,
                         e.LastOperation, 
                         e.SocketError));

                    if (handler != null && handler.ClientStatus != (int)ClientStateEnums.Closed)
                    {
                        handler.ClientStatus = (int)ClientStateEnums.Closed;

                        // 通知外部事件
                        if (this.ClientDisconnected != null)
                        {
                            ClientStateEventArgs stateObject = new ClientStateEventArgs(e);
                            stateObject.ClientStatus = ClientStateEnums.Closed;

                            this.ClientDisconnected(this, stateObject);
                        }

                        this.RecycleContext(e);

                    }

                    handler.Dispose();
                }
            }
        }


        #endregion


        #region 清理工作

        /// <summary>
        /// 关闭SocketAsyncEventArg相关联的Socket
        /// </summary>
        /// <param name="e">完成收/发操作的SocketAsyncEventArg对象</param>
        public void RecycleContext(SocketAsyncEventArgs e)
        {

            // 1.从客户列表中移除
            _clientMembers.Remove(e.UserToken as ITcpClientProxy);

            // 回收之前判断是否已回收....暂时没找到好的解决方法
            if (!IsSocketAlive(e))
                return;

            // 在销毁之前，记录一些值
            string socketToken = e.AcceptSocket.RemoteEndPoint.ToString();
            SocketAsyncOperation lastOp = e.LastOperation;

            try
            {
                // 在Close之前调用Shutdown
                e.AcceptSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
                // 如果Socket已经关闭
            }
            finally
            {
                e.AcceptSocket.Close();
            }


            // 回收SocketAsyncEventArg以便重用于另一个客户端.
            if (lastOp == SocketAsyncOperation.Receive)
            {
                e.Completed -= this.SocketAsyncEventArgs_IOCompleted;
                this._poolOfReadWriteEventArgs.Push(e);
            }
            //else if (lastOp == SocketAsyncOperation.Send)
            //{  
            //    this._sendPool.Push(e);
            //}
            else
            {
                e.Dispose();
            }

            // 释放Semaphore，在回收SAEA对象之后调用
            this._semaphoreAcceptedClients.Release();

            // 输出
            _logger.Info(string.Format("客户端 {0} 断开连接. 还有{1}个连接。", socketToken, this.ConnectedCount));
           

        }

        /// <summary>
        /// 关闭心跳检查线程
        /// </summary>
        private void StopCheckingPulse()
        {
            if (_pulseWorkThread != null)
            {
                _pulseWorkThread.Close();
            }
        }

        #endregion

        #endregion

        #region 可重写的成员

        // 客户端连接
        protected virtual void OnClientConnected(SocketAsyncEventArgs e)
        {
            if (this.ClientConnected != null)
            {
                ClientStateEventArgs stateObject = new ClientStateEventArgs(e);
                ClientConnected(this, stateObject);
            }
        }

        // 客户端断开
        protected virtual void OnClientDisconnected(SocketAsyncEventArgs e)
        {
            if (this.ClientDisconnected != null)
            {
                ClientStateEventArgs stateObject = new ClientStateEventArgs(e);
                this.ClientDisconnected(this, stateObject);
            }
        }

        // 从客户端接收消息后
        protected virtual void OnMessageReceived(SocketAsyncEventArgs e)
        {
            if (this.MessageReceived != null)
            {
                ClientStateEventArgs stateObject = new ClientStateEventArgs(e);
                stateObject.ClientStatus = ClientStateEnums.Connected; 
                                
                this.MessageReceived(this, stateObject);

                // 保存服务器要返回给客户端的消息（临时）
                stateObject.ClientProxy.ExchangedData = stateObject.ResponseMessage;
            }
        }

        // 向客户端发送消息后
        protected virtual void OnMessageSend(SocketAsyncEventArgs e)
        {
            if (this.MessageSend != null)
            {
                ClientStateEventArgs stateObject = new ClientStateEventArgs(e);
                stateObject.ClientStatus = ClientStateEnums.Connected; 

                this.MessageSend(this, stateObject);
            }
        }
        

        #endregion

        #region SocketAsyncEventArgs事件处理

        /// <summary>
        /// 用于Socket的AcceptAsync操作完成时回调的事件处理
        /// 如果Accept操作是同步完成的（或某种错误），不会引发此事件。
        /// </summary>
        /// <param name="sender">引用事件的对象</param>
        /// <param name="e">与完成Accept操作关联的SocketAsyncEventArg对象</param>
        protected virtual void SocketAsyncEventArgs_AcceptAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            lock (_tcpLock)
            {
                if (e.SocketError == SocketError.OperationAborted)
                    return;

                this.ProcessAccept(e);
            }
        }

        /// <summary>
        /// 用于ReceiveAsync或SendAsync操作完成时回调Completed的事件处理
        /// </summary>
        /// <param name="sender">引用事件的对象</param>
        /// <param name="e">与完成Receive/Send操作关联的SocketAsyncEventArg对象</param>
        protected virtual void SocketAsyncEventArgs_IOCompleted(object sender, SocketAsyncEventArgs e)
        {
            lock (_tcpLock)
            {
                // LastOperation默认为None，直到使用xxxAsync方法设置。
                // LastOperation值在SocketAsyncEventArgs实例中保持有效，直到该实例用于另一个异步套接字（xxxAsync)操作。
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Receive:
                        this.ProcessReceive(e);
                        break;
                    case SocketAsyncOperation.Send:
                        this.ProcessSend(e);
                        break;
                    default:
                        Trace.WriteLine("Socket的上一操作非读写操作:" + e.LastOperation.ToString());
                        break;
                }
            }
            
        }

        #endregion


    }
}
