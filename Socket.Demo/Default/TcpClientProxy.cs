using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Globalization;
using System.Diagnostics;
using Sockets.Interfaces;

namespace Sockets.Default
{
    /// <summary>
    /// 用于上下文交换数据的自定义类型
    /// </summary>
    public class TcpClientProxy : ITcpClientProxy, IDisposable
    {
        #region 委托

        public event Func<ITcpClientProxy, Boolean> ValidatingHandler;

        #endregion

        #region 字段

        // 用于事件锁
        private readonly object raiseLock = new object();

        public bool IsDisposed { get; set; }

        protected System.Net.Sockets.Socket _clientSocket;
        
        protected Encoding _encoding;
        
        // 存放来自客户端发送的消息缓冲区
        private byte[] _clientBuffer;
        // 当前缓冲区读写的位置
        private int _bufferReceiveOffset;

        #endregion

        #region 属性

        public int ClientStatus { get; set; }

        public List<IPackageInfo> LastPackages { get; set; }

        public string LastReceivedString { get; set; }

        public byte[] LastReceivedBytes { get; set; }

        public string GUID
        {
            get;
            set;
        }
         
        /// <summary>
        /// 表示业务唯一标识
        /// </summary>
        public string BusinessID { get; set; }

        public object ExchangedData { get; set; }

        protected object UserToken { get; set; }


        public SocketAsyncEventArgs ReceiveContext { get; set; }

        /// <summary>
        /// 接收的Socket
        /// </summary>
        public System.Net.Sockets.Socket Connection
        {
            get { return this._clientSocket; }
        }

        /// <summary>
        /// 最后一次接收时间
        /// </summary>
        public DateTime FeedbackTime
        {
            get;
            set;
        }
        public IPackageOperater DefaultOperater { get; protected set; }



        #endregion

        #region 构造

        /// <summary>
        /// 实例化ClientHandler
        /// </summary>
        /// <param name="connection">接收消息的Socket.</param>
        /// <param name="bufferSize">接收数据的缓冲大小.</param>
        //internal ClientHandler(Socket connection, Int32 bufferSize, Encoding encoding, SocketAsyncEventArgs context)
        //{

        //    // ClientUID
        //    this.Client = new ClientInfo(System.Guid.NewGuid().ToString("N"));

        //    this.connection = connection;
        //    this.encoding = encoding;
        //}

        public TcpClientProxy(System.Net.Sockets.Socket connection, IPackageSettings settings)
        {
            // ClientUID
            //this.Client = new ClientInfo(System.Guid.NewGuid().ToString("N"));

            this.GUID = System.Guid.NewGuid().ToString("N");
            _clientSocket = connection;
            _clientBuffer = new byte[settings.BufferSize];
            _encoding = settings.Encoding;
            
            // Default
            byte[] prefixBytes = Encoding.Default.GetBytes(settings.PrefixString);
            int length = Convert.ToInt32(settings.LengthString);
            DefaultOperater = new PackageOperater(prefixBytes, length);
        }


        #endregion

        #region Public方法

        public bool IsValidated()
        {
            bool isValidated = false;
            if (ValidatingHandler != null)
            {
                isValidated = ValidatingHandler(this);
            }

            return isValidated;
        }

        /// <summary>
        /// 交换数据过期
        /// </summary>
        public void ExchangeExpire()
        {
            this.UserToken = null;
        }

        /// <summary>
        /// 设置交换数据
        /// </summary>
        /// <param name="userToken"></param>
        public void SetExchange(object userToken)
        {
            this.UserToken = userToken;
        }

        /// <summary>
        /// 获取交换数据
        /// </summary>
        /// <returns></returns>
        public object GetExchange()
        {
            return this.UserToken;
        }

        #endregion
        
        #region Internal方法


        public void PrepareOutgoingData(SocketAsyncEventArgs e, byte[] bufferToSend)
        {
            // 清空
            Array.Clear(e.Buffer, e.Offset, e.Buffer.Length - e.Offset);

            // 要发送的内容 
            //Byte[] bufferToSend = encoding.GetBytes(content);

            e.SetBuffer(e.Offset, bufferToSend.Length);

            Buffer.BlockCopy(bufferToSend, 0, e.Buffer, e.Offset, bufferToSend.Length);
        }

        /// <summary>
        /// 是否接收完毕
        /// </summary>
        /// <returns></returns>
        internal bool IsComplete()
        {
            // 使用 Available 来确定数据是否排队等待读取, 为0表示没有排队
            if (_clientSocket != null && _clientSocket.Available == 0)
            {
                return true;
            }

            return false;
        }


        internal bool Echo(SocketAsyncEventArgs args)
        {
            int start = args.Offset;
            int length = _bufferReceiveOffset;
            int total = args.Buffer.Length;
            if (length > total)
                throw new Exception("数据超出缓冲大小。");

            // SetBuffer修改Offset和Count属性，指定要接收或发送的缓冲区范围。此方法不会更改Buffer属性。
            // 初始化时才调用三个参数的方法，这里调用是错的，会覆盖掉原来设置的缓冲区大小(除非双方交互都是使用固定长度的协议)
            args.SetBuffer(start, length);

            // 发送回客户端
            bool async = _clientSocket.SendAsync(args);
            
            //
            //args.SetBuffer(start, total);

            return async;
        }

        
        public bool TryReceivePackages(SocketAsyncEventArgs ioContext, out List<IPackageInfo> packages)
        {
            this.UpdateStatus();
            packages = null;
            
            int incomingByteLength = ioContext.BytesTransferred;
            if (_bufferReceiveOffset + incomingByteLength > _clientBuffer.Length)
            {
                throw new Exception("超出缓冲区大小。");
            }
            
            // 将本次接收的消息是放入协议包
            byte[] receivingBytes = new byte[incomingByteLength];            
            Buffer.BlockCopy(ioContext.Buffer,  // src Array
                ioContext.Offset,               // srcOffset
                receivingBytes,                 // dst
                0,                              // dstOffset
                incomingByteLength               // count
            );
            DefaultOperater.Push(receivingBytes);


            // 将本次接收的消息放入缓存（针对非协议内容的数据，方便处理）
            //Console.WriteLine("接收数据:");
            //foreach (byte b in receivingBytes)
            //{
            //    Console.Write(b.ToString("D2"));
            //    Console.Write(" ");
            //}
            //Console.WriteLine();

            //Console.WriteLine(Encoding.Default.GetString(receivingBytes));
            Buffer.BlockCopy(ioContext.Buffer,  
                ioContext.Offset,               
                _clientBuffer,                 
                _bufferReceiveOffset,                              
                incomingByteLength               
            );
            _bufferReceiveOffset += incomingByteLength;
            
            // 将本次接收的数据公开出去
            this.LastReceivedString = _encoding.GetString(_clientBuffer);
            this.LastReceivedBytes = new byte[_bufferReceiveOffset];
            Buffer.BlockCopy(_clientBuffer,
                0,
                LastReceivedBytes,
                0,
                _bufferReceiveOffset
            );


            bool finish = this.IsComplete();
            if (finish)
            {

                // 将本次接收的消息放入缓存（针对非协议内容的数据，方便处理）
                Console.WriteLine("接收数据:");
                try
                {
                    foreach (byte b in receivingBytes)
                    {
                        Console.Write(b.ToString("D2"));
                        Console.Write(" ");
                    }
                    Console.WriteLine();
                }
                catch(Exception ex)
                {
                    throw ex;
                }
                // 解析封包，保存解析结果
                try
                {
                    packages = DefaultOperater.HandOver();
                }
                catch
                {
                    packages = null;
                }
                this.LastPackages = packages;

                // 清零
                _bufferReceiveOffset = 0;
                Array.Clear(_clientBuffer, 0, _clientBuffer.Length);

            }
            else
            {
                //packages = null;
            }


            return finish;
        }


        #endregion
        
        #region 虚方法

        protected virtual void UpdateStatus()
        {
            //if (LastMessage == null)
            //{
            //    LastMessage = new MessageInfo(Guid.NewGuid().ToString("N"));
            //}

            this.FeedbackTime = DateTime.Now;
        }

        #endregion
        
        #region IDisposable 成员

        /// <summary>
        /// 释放对象.
        /// </summary>
        public void Dispose()
        {
            IsDisposed = true;


            // test for debug
            if (this._clientSocket != null)
            {
                try
                {
                    //this.connection.Shutdown(SocketShutdown.Send);

                    //this._clientSocket.Shutdown(SocketShutdown.Both);
                    //this._clientSocket.Disconnect(false);
                }
                catch (Exception)
                {
                    // 如果客户端已关闭连接
                }
                finally
                {
                    if (this._clientSocket.Connected)
                    {
                        this._clientSocket.Close();
                    }
                    this._clientSocket = null;
                }
            }



        }

        #endregion
    }
}
