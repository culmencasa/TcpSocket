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
    /// Tcp����
    /// �ο�MSDNԴ��: http://msdn2.microsoft.com/zh-cn/library/system.net.sockets.socketasynceventargs.aspx
    /// </summary>
    public class TcpServer : ITcpServer
    {
        #region ί�к��¼�

        public event EventHandler<ClientStateEventArgs> ClientConnected;
        public event EventHandler<ClientStateEventArgs> ClientDisconnected;
        
        public event EventHandler<ClientStateEventArgs> MessageSend;
        public event EventHandler<ClientStateEventArgs> MessageReceived;

        public Func<Socket, IPackageSettings, ITcpClientProxy> CreateClientHandler;



        #endregion

        #region ��̬��Ա

        /// <summary>
        /// ����д����Ϊ���շ��仺�壩
        /// </summary>
        const int opsToPreAlloc = 2;     

        /// <summary>
        /// ���ڷ�����ִ�еĻ���ͬ������.
        /// </summary>
        protected static Mutex mutex = new Mutex();

        #endregion

        #region �ֶ�

        /// <summary>
        /// tcp������ս��
        /// </summary>
        private IPEndPoint _serverEndPoint;

        /// <summary>
        /// ��־����
        /// </summary>
        protected EZLogger _logger;

        /// <summary>
        /// �����ڷ���/������Ϣ�ı����ʽ
        /// </summary>
        protected Encoding _bufferEncoding;


        /// <summary>
        /// ���ڼ������������Socket.
        /// </summary>
        protected Socket _socketListener;

        /// <summary>
        /// ����ÿ��I/O Socket�����Ļ�������С.
        /// </summary>
        protected int _receiveBufferSize;

        /// <summary>
        /// �������Ӷ��е���󳤶�
        /// </summary>
        protected int _backlog;

        /// <summary>
        /// ��ʾһ�����������׽��ֲ����Ŀ����û���
        /// </summary>
        private static BufferManager _bufferManager;


        /// <summary>
        /// ���ڽ������ӵ�SocketAsyncEventArgs�����
        /// </summary>
        protected SocketAsyncEventArgsPool _poolOfAcceptEventArgs;

        /// <summary>
        /// ���ڽ����շ���Ϣ��SocketAsyncEventArgs�����
        /// </summary>
        protected SocketAsyncEventArgsPool _poolOfReadWriteEventArgs;


        /// <summary>
        /// �������ӵ��������Ŀͻ�����.
        /// </summary>
        protected Semaphore _semaphoreAcceptedClients;

        /// <summary>
        /// �ͻ��˼���
        /// </summary>
        protected TcpClientProxyList _clientMembers; //todo: ��Ϊ�ⲿ����չ�Ķ���

        /// <summary>
        /// �����߳�
        /// </summary>
        private PulseCheckThread _pulseWorkThread;

        /// <summary>
        /// ������
        /// </summary>
        private object _tcpLock = new object();

        #endregion

        #region ����

        /// <summary>
        /// �������ϵ���������
        /// </summary>
        public int ConnectedCount
        {
            get
            {
                return _clientMembers.Count;
            }
        }

        /// <summary>
        /// �Ƿ�ش��ͻ�����Ϣ(����Է�Э����Ϣ����������)
        /// </summary>
        public bool Echo
        {
            get;
            set;
        }

        /// <summary>
        /// ��ȡ�����ÿͻ���ʱ������
        /// </summary>
        public int Timeout
        {
            get;
            set;
        }

        /// <summary>
        /// �ͻ������༯��
        /// </summary>
        public IList<ITcpClientProxy> GetClientCollections()
        {
            return _clientMembers.AsReadOnly();
        }

        /// <summary>
        /// �������ս��
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

        #region ����

        /// <summary>
        /// ʵ����һ���������ӵ�����������, ��ָ�������ʽ������������Ͷ�д�����С
        /// </summary>
        /// <param name="maxConnectionNumber">������ͬʱ����������������.</param>
        /// <param name="receiveBufferSize">ÿ��I/O�����Ļ����С.</param>
        public TcpServer(IPackageSettings setting)
        {
            // ������־
            StartLog();

            _bufferEncoding = setting.Encoding;
            _backlog = setting.MaxConnectionNumber;
            _receiveBufferSize = setting.BufferSize;

            _semaphoreAcceptedClients = new Semaphore(_backlog, _backlog);
            _poolOfAcceptEventArgs = new SocketAsyncEventArgsPool(_backlog);
            _poolOfReadWriteEventArgs = new SocketAsyncEventArgsPool(_backlog);
            //_sendPool = new SocketAsyncEventArgsPool(this._backlog);

            this.Setting = setting;

            // ����������ʱ
            Timeout = 30; // Ĭ��30��

            _clientMembers = new TcpClientProxyList();

            InitializeContexts();
        }

        #endregion

        #region ��������

        /// <summary>
        /// ��������������������.
        /// </summary>
        public void Start(IPEndPoint localEndPoint)
        {
            try
            {
                // 1.������������
                _socketListener = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _socketListener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _socketListener.ReceiveBufferSize = this._receiveBufferSize;
                _socketListener.SendBufferSize = this._receiveBufferSize;

                // 2.��IPv4��IPv6
                if (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    _serverEndPoint = new IPEndPoint(IPAddress.IPv6Any, localEndPoint.Port);

                    // 27 ��winsock���ʾ IPV6_V6ONLY ѡ��, ���
                    // http://blogs.msdn.com/wndp/archive/2006/10/24/creating-ip-agnostic-applications-part-2-dual-mode-sockets.aspx
                    _socketListener.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                    _socketListener.Bind(_serverEndPoint);


                }
                else
                {
                    _serverEndPoint = localEndPoint;
                    _socketListener.Bind(localEndPoint);
                }

                // 3.�������������TCP���ӡ� backlog ����ָ���������������ɵĵȴ����ܵĴ����������� 
                _socketListener.Listen(_backlog);

                this.StartAccept();

                this.StartCheckingPulse();

                // ������ǰ�̣߳����մ������Ϣ. 
                mutex.WaitOne();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.StackTrace);
                throw new Exception("����Tcp����ʧ�ܡ�");
            }
        }

        /// <summary>
        /// ֹͣ��������.
        /// </summary>
        public void Stop()
        {
            _logger.Info("�����ر�TCP, �Ͽ�������");


            // ��֪ͨ���пͻ��˶Ͽ�
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
                        // �ⲿδ������쳣����¼��־����Ӱ�������̡�
                        _logger.Error(ex.StackTrace);
                    }

                    //this.RecycleContext(clientHandler.ReceiveContext);
                }
            }

            lock (_tcpLock)
            {
                // 1.pulse thread close
                this.StopCheckingPulse();

                // �����棬�¼�
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


                // �����ǰ�ͻ���
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
        /// �����Ƿ������� (�ĳ�ö��)
        /// </summary>
        /// <returns></returns>
        public bool IsServerRunning()
        {
            if (_socketListener == null)
                return false;

            return true;
        }

        /// <summary>
        /// ͬ��������Ϣ���ͻ���
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
                    _logger.Error("�ͻ����ѶϿ���");
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
        /// �첽������Ϣ���ͻ���
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
                        #region �첽������ϴ���

                        switch (e.LastOperation)
                        {
                            case SocketAsyncOperation.Send:
                                if (context.SocketError == SocketError.Success)
                                {
                                    // ���÷����¼�
                                    this.OnMessageSend(context);
                                }
                                else
                                {
                                    // �����г�������
                                    context.Completed -= processAsyncSend;
                                    this.ProcessError(context);
                                    return;
                                }
                                break;
                        }

                        #endregion

                        // ������첽������ϣ������
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

            // ���ͻؿͻ���
            if (!context.AcceptSocket.SendAsync(context))
            {
                #region ͬ���������

                processAsyncSend(this, context);

                #endregion
            }

        }


        /// <summary>
        /// �����ϴν��ղ�����ϵ������Ķ�����������Ϣ
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

        #region ��������

        #region ׼������

        /// <summary>
        /// ������־
        /// </summary>
        protected void StartLog()
        {
            // ������־
            _logger = new EZLogger(
                Path.Combine(Environment.CurrentDirectory, "Logs\\TcpServerLog_" + DateTime.Now.ToString("yyyyMMdd") + ".log"),
                true,
                (uint)EZLogger.Level.All);
            _logger.Start();

        }

        /// <summary>
        /// Ԥ��������õĻ������Լ������Ķ����Ա���߷��������ܡ�
        /// </summary>
        protected virtual void InitializeContexts()
        {
            if (_bufferManager == null)
            {
                // ���仺���������������������׽��ֵĶ�д  
                _bufferManager = new BufferManager(
                    _receiveBufferSize * _backlog * opsToPreAlloc, 
                    //this._receiveBufferSize * this._backlog,
                    _receiveBufferSize);
            }
            _bufferManager.InitBuffer();

            // ���ڽ������ӵ�SAEA������Ҫ�趨������
            for (Int32 i = 0; i < this._backlog; i++)
            { 
                SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
                // ����Popʱ��ӣ�����ʱɾ���¼�
                //acceptEventArg.Completed += this.SocketAsyncEventArgs_IOCompleted;

                this._poolOfAcceptEventArgs.Push(acceptEventArg);
            }           

            // Ԥ����SocketAsyncEventArgs����أ��������¼�����
            for (Int32 i = 0; i < this._backlog * opsToPreAlloc; i++)
            {
                SocketAsyncEventArgs readWriteEventArg = new SocketAsyncEventArgs();
                // ����Popʱ��ӣ�����ʱɾ���¼�
                //readWriteEventArg.Completed += this.SocketAsyncEventArgs_IOCompleted;

                _bufferManager.SetBuffer(readWriteEventArg);

                this._poolOfReadWriteEventArgs.Push(readWriteEventArg);
            }


            // ��ʱ���ã���Ϊ�첽����ʱ����Ͷ���
            //for (Int32 i = 0; i < this._backlog; i++)
            //{
            //    SocketAsyncEventArgs sendEventArg = new SocketAsyncEventArgs();
            //    sendEventArg.Completed += this.SocketAsyncEventArgs_IOCompleted;
            //    _bufferManager.SetBuffer(sendEventArg);

            //    this._sendPool.Push(sendEventArg);
            //}
        }

        /// <summary>
        /// ��ʼ��������
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
        ///  ������ڵĿͻ���
        /// </summary>
        /// <param name="expiredClientList"></param>
        private void HandleExpiredClients(IList<ITcpClientProxy> expiredClientList)
        {
            if (expiredClientList != null)
            {
                for (int index = expiredClientList.Count - 1; index >= 0; index--)
                {
                    var clientHandler = expiredClientList[index];

                    _logger.Error(string.Format("BusinessIdΪ{0}��ѧ����������ʱ�����Ͽ�������", clientHandler.BusinessID));

                    // ֪ͨ�ⲿ�¼�
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
                        // �ⲿδ������쳣����¼��־����Ӱ�������̡�
                        _logger.Error(ex.StackTrace);
                    }

                    this.RecycleContext(clientHandler.ReceiveContext); 
                }
            }
        }


        #endregion

        #region SocketAsyncEventArgs���

        /// <summary>
        /// �ж�Sockte�Ƿ�
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
        /// ���������ͻ������Ӳ���. 
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
                    // �ظ�������ͨ��ProcessAccept()�����ж�StartAccept->ProcessAccept->StartAccept�ĵݹ�ʵ�ֵ�

                    this.ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// ���������ͻ������Ӳ���. 
        /// </summary>
        /// <param name="reuseContext">Ҫ�����������ӵ����������Ķ���</param>
        protected void StartAccept(SocketAsyncEventArgs reuseContext)
        {
            if (!IsServerRunning())
                return;

            // 1.����SocketAsyncEventArgs����
            // �ڵ��� Socket.AcceptAsync ����֮ǰδ�ṩSocket������Ϊ null����.NET���Զ�����һ���µ�Socket
            // �������SocketAsyncEventArgs������Ҫ���Socket
            reuseContext.AcceptSocket = null; 

            // 2.���ڿ������ӵĸ���
            this._semaphoreAcceptedClients.WaitOne();

            // 3.��ʼ�첽��������(Accept)����
            // AcceptAsync()�������false��ʾ������ͬ����ɵģ���������SocketAsyncEventArgs.Completed�¼�
            if (!this._socketListener.AcceptAsync(reuseContext))
            {
                this.ProcessAccept(reuseContext);
            }
        }

        /// <summary>
        /// �����������ݲ���
        /// </summary>
        /// <param name="readContext"></param>
        protected void StartReceive(SocketAsyncEventArgs readContext)
        {
            if (!IsSocketAlive(readContext))
                return;

            // ����Ҫ���յ�λ�úʹ�С(ÿ�ζ����ã��Է�ֹ�����ط��޸���SocketAsyncEventArgs��ȫ��Buffer���λ�ã�
            readContext.SetBuffer(readContext.Offset, readContext.Buffer.Length - readContext.Offset);     
            bool willRaiseEvent = readContext.AcceptSocket.ReceiveAsync(readContext);
            if (!willRaiseEvent)
            {
                this.ProcessReceive(readContext);
            }
        }

        /// <summary>
        /// �������ݲ���
        /// </summary>
        /// <param name="e"></param>
        private void StartSend(SocketAsyncEventArgs e)
        {
            if (!IsSocketAlive(e))
                return;

            // ���ͻؿͻ���
            if (!e.AcceptSocket.SendAsync(e))
            {
                this.ProcessSend(e);
            }
        }


        /// <summary>
        /// ������������
        /// </summary>
        /// <param name="acceptContext">�����Accept����������SocketAsyncEventArg����</param>
        private void ProcessAccept(SocketAsyncEventArgs acceptContext)
        {
            // 1.�µ�����
            Socket acceptSocket = acceptContext.AcceptSocket;
            if (acceptSocket != null && acceptContext.SocketError == SocketError.Success)
            {
                try
                {
                    // �Ӷ������ȡ��һ�����ڶ�ȡ��SocketAsyncEventArgs���󣬽����ӵ���Socket�ͻ��˷���UserToken
                    SocketAsyncEventArgs readEventArgs = _poolOfReadWriteEventArgs.Pop();
                    if (readEventArgs == null)
                    {
                        Trace.WriteLine(string.Format("�������������ܾ� {0} ����������", acceptSocket.RemoteEndPoint));
                        acceptSocket.Close();
                        return;
                    }

                    readEventArgs.Completed += SocketAsyncEventArgs_IOCompleted;


                    ITcpClientProxy clientHandler = null;

                    #region ����������Ϣ

                    if (CreateClientHandler != null)
                    {
                        clientHandler = CreateClientHandler(acceptSocket, Setting);
                        clientHandler.FeedbackTime = DateTime.Now;
                        clientHandler.ReceiveContext = readEventArgs;
                        clientHandler.ClientStatus = (int)ClientStateEnums.Connected;
                    }

                    #endregion

                    //������SAEA��������Socket���ݸ������շ���SAEA
                    readEventArgs.AcceptSocket = acceptSocket;
                    readEventArgs.UserToken = clientHandler;

                    // ����������
                    if (clientHandler != null)
                    {
                        _clientMembers.Add(clientHandler);
                    }

                    _logger.Info(string.Format("{0} {1}�����Ͻ�ʦ��, Ŀǰ����������{2}�����ӡ�",
                        DateTime.Now.ToString("HH:mm:ss"),
                        acceptSocket.RemoteEndPoint,
                        this.ConnectedCount));
                        
                    // ֪ͨ�ⲿ�����µ�����
                    OnClientConnected(readEventArgs);

                    // ��ʼ���������Ӵ��������
                    this.StartReceive(readEventArgs);
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine("����ʱ��������" + ex.StackTrace);
                    _logger.Error("��������ʱ����" + ex.StackTrace);
                }
            }
            else
            {
                 //Close�����رղ��ͷ�������Դ(�йܺͷ��й�), �ڲ�������Dispose
                if (acceptSocket != null)
                {
                    acceptSocket.Close();
                }
                _semaphoreAcceptedClients.Release();
            }


            #region ���Ĵ��������¸�����

            // ��ʼ�����µ����ӣ��������������õ�ǰ��acceptContext�����������ܣ�
            this.StartAccept();

            // ��SAEA����Ż����ӳأ��Ա�����
            acceptContext.Completed -= this.SocketAsyncEventArgs_IOCompleted;
            acceptContext.AcceptSocket = null; // 2015-05-20
            _poolOfAcceptEventArgs.Push(acceptContext);

            #endregion

        }
        
        /// <summary>
        /// ������ղ���
        /// ��һ���첽���ղ������ʱ���ô˷���
        /// </summary>
        /// <param name="receiveContext">�����Receive����������SocketAsyncEventArg����</param>
        private void ProcessReceive(SocketAsyncEventArgs receiveContext)
        {

            if (!IsSocketAlive(receiveContext))
                return;

            // BytesTransferred�����ṩ�ڿɽ��ջ������ݵ��첽�׽��ֲ���������ֽ�����
            // ����Ӷ�ȡ���������㣬��˵��Զ�̶��ѹر�������
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
                            // ������Ϣ��������¼�
                            try
                            {
                                this.OnMessageReceived(receiveContext);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex.StackTrace);
                            }

                            // ������Ӧ��Ϣ
                            // review: ����Ӧ����TryReceivePackages���صļ���?
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
                            ////todo: ��Ϣ�ֶη���
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
                            // ���û�ж�ȡ�꣬����������һ������
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
                    Console.WriteLine("Socket����ʱ����, ����:" + receiveContext.SocketError);
                    _logger.Error("Socket����ʱ����, ����:" + receiveContext.SocketError);

                    this.ProcessError(receiveContext);
                }
                

            }
            else
            {
                // ���Զ�̶˹ر������ӣ�����Ҳ�ر�Socket
                Console.WriteLine("ƽ��˹ر�������.");
                _logger.Error("ƽ��˹ر�������.");
                this.ProcessError(receiveContext);
            }
        }

        /// <summary>
        /// �����Ͳ���
        /// ��һ�����Ͳ������ʱ���ô˷���  
        /// </summary>
        /// <param name="context">�����Send����������SocketAsyncEventArg����.</param>
        private void ProcessSend(SocketAsyncEventArgs context)
        {
            if (context.SocketError == SocketError.Success)
            {
                ITcpClientProxy token = context.UserToken as ITcpClientProxy;
                
                //todo: ����Ϣ�ֶη�
                //receiveSendToken.sendBytesRemainingCount = token.sendBytesRemainingCount - receiveSendEventArgs.BytesTransferred;


                // MSDN������
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


                // ���÷����¼�
                this.OnMessageSend(context);


                // ��ȡ�ͻ��˷��͹�������һ����Ϣ.
                StartReceive(context);
            }
            else
            {
                Console.WriteLine("Socket����ʱ���󣬴���:" + context.SocketError);
                _logger.Error("Socket����ʱ���󣬴���:" + context.SocketError);
                this.ProcessError(context);
            }
        }
        
        /// <summary>
        /// ����������
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
                    
                    Console.WriteLine("{0} �����ˣ�����֮��{1} : {2}", 
                        e.RemoteEndPoint,
                         e.LastOperation, 
                         e.SocketError
                       );
                    _logger.Error(string.Format("������һ���ͻ���{0}��{1} : {2}", 
                        e.RemoteEndPoint,
                         e.LastOperation, 
                         e.SocketError));

                    if (handler != null && handler.ClientStatus != (int)ClientStateEnums.Closed)
                    {
                        handler.ClientStatus = (int)ClientStateEnums.Closed;

                        // ֪ͨ�ⲿ�¼�
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


        #region ������

        /// <summary>
        /// �ر�SocketAsyncEventArg�������Socket
        /// </summary>
        /// <param name="e">�����/��������SocketAsyncEventArg����</param>
        public void RecycleContext(SocketAsyncEventArgs e)
        {

            // 1.�ӿͻ��б����Ƴ�
            _clientMembers.Remove(e.UserToken as ITcpClientProxy);

            // ����֮ǰ�ж��Ƿ��ѻ���....��ʱû�ҵ��õĽ������
            if (!IsSocketAlive(e))
                return;

            // ������֮ǰ����¼һЩֵ
            string socketToken = e.AcceptSocket.RemoteEndPoint.ToString();
            SocketAsyncOperation lastOp = e.LastOperation;

            try
            {
                // ��Close֮ǰ����Shutdown
                e.AcceptSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
                // ���Socket�Ѿ��ر�
            }
            finally
            {
                e.AcceptSocket.Close();
            }


            // ����SocketAsyncEventArg�Ա���������һ���ͻ���.
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

            // �ͷ�Semaphore���ڻ���SAEA����֮�����
            this._semaphoreAcceptedClients.Release();

            // ���
            _logger.Info(string.Format("�ͻ��� {0} �Ͽ�����. ����{1}�����ӡ�", socketToken, this.ConnectedCount));
           

        }

        /// <summary>
        /// �ر���������߳�
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

        #region ����д�ĳ�Ա

        // �ͻ�������
        protected virtual void OnClientConnected(SocketAsyncEventArgs e)
        {
            if (this.ClientConnected != null)
            {
                ClientStateEventArgs stateObject = new ClientStateEventArgs(e);
                ClientConnected(this, stateObject);
            }
        }

        // �ͻ��˶Ͽ�
        protected virtual void OnClientDisconnected(SocketAsyncEventArgs e)
        {
            if (this.ClientDisconnected != null)
            {
                ClientStateEventArgs stateObject = new ClientStateEventArgs(e);
                this.ClientDisconnected(this, stateObject);
            }
        }

        // �ӿͻ��˽�����Ϣ��
        protected virtual void OnMessageReceived(SocketAsyncEventArgs e)
        {
            if (this.MessageReceived != null)
            {
                ClientStateEventArgs stateObject = new ClientStateEventArgs(e);
                stateObject.ClientStatus = ClientStateEnums.Connected; 
                                
                this.MessageReceived(this, stateObject);

                // ���������Ҫ���ظ��ͻ��˵���Ϣ����ʱ��
                stateObject.ClientProxy.ExchangedData = stateObject.ResponseMessage;
            }
        }

        // ��ͻ��˷�����Ϣ��
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

        #region SocketAsyncEventArgs�¼�����

        /// <summary>
        /// ����Socket��AcceptAsync�������ʱ�ص����¼�����
        /// ���Accept������ͬ����ɵģ���ĳ�ִ��󣩣������������¼���
        /// </summary>
        /// <param name="sender">�����¼��Ķ���</param>
        /// <param name="e">�����Accept����������SocketAsyncEventArg����</param>
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
        /// ����ReceiveAsync��SendAsync�������ʱ�ص�Completed���¼�����
        /// </summary>
        /// <param name="sender">�����¼��Ķ���</param>
        /// <param name="e">�����Receive/Send����������SocketAsyncEventArg����</param>
        protected virtual void SocketAsyncEventArgs_IOCompleted(object sender, SocketAsyncEventArgs e)
        {
            lock (_tcpLock)
            {
                // LastOperationĬ��ΪNone��ֱ��ʹ��xxxAsync�������á�
                // LastOperationֵ��SocketAsyncEventArgsʵ���б�����Ч��ֱ����ʵ��������һ���첽�׽��֣�xxxAsync)������
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Receive:
                        this.ProcessReceive(e);
                        break;
                    case SocketAsyncOperation.Send:
                        this.ProcessSend(e);
                        break;
                    default:
                        Trace.WriteLine("Socket����һ�����Ƕ�д����:" + e.LastOperation.ToString());
                        break;
                }
            }
            
        }

        #endregion


    }
}
