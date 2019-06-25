using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sockets
{
    /// <summary>
    /// 简单Http服务类
    /// </summary>
    public class SimpleHttpServer
    {
        #region 默认页

        private static readonly string[] _indexFiles =
        {
            "index.html",
            "index.htm",
            "default.html",
            "default.htm"
        };

        #endregion

        #region 字段

        // MIME注册
        private static IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
                #region MIME类型

                {".asf", "video/x-ms-asf"},
                {".asx", "video/x-ms-asf"},
                {".avi", "video/x-msvideo"},
                {".bin", "application/octet-stream"},
                {".cco", "application/x-cocoa"},
                {".crt", "application/x-x509-ca-cert"},
                {".css", "text/css"},
                {".deb", "application/octet-stream"},
                {".der", "application/x-x509-ca-cert"},
                {".dll", "application/octet-stream"},
                {".dmg", "application/octet-stream"},
                {".ear", "application/java-archive"},
                {".eot", "application/octet-stream"},
                {".exe", "application/octet-stream"},
                {".flv", "video/x-flv"},
                {".gif", "image/gif"},
                {".hqx", "application/mac-binhex40"},
                {".htc", "text/x-component"},
                {".htm", "text/html"},
                {".html", "text/html"},
                {".ico", "image/x-icon"},
                {".img", "application/octet-stream"},
                {".iso", "application/octet-stream"},
                {".jar", "application/java-archive"},
                {".jardiff", "application/x-java-archive-diff"},
                {".jng", "image/x-jng"},
                {".jnlp", "application/x-java-jnlp-file"},
                {".jpeg", "image/jpeg"},
                {".jpg", "image/jpeg"},
                {".js", "application/x-javascript"},
                {".mml", "text/mathml"},
                {".mng", "video/x-mng"},
                {".mov", "video/quicktime"},
                {".mp3", "audio/mpeg"},
                {".mp4", "video/mpeg"},
                {".mpeg", "video/mpeg"},
                {".mpg", "video/mpeg"},
                {".msi", "application/octet-stream"},
                {".msm", "application/octet-stream"},
                {".msp", "application/octet-stream"},
                {".pdb", "application/x-pilot"},
                {".pdf", "application/pdf"},
                {".pem", "application/x-x509-ca-cert"},
                {".pl", "application/x-perl"},
                {".pm", "application/x-perl"},
                {".png", "image/png"},
                {".prc", "application/x-pilot"},
                {".ra", "audio/x-realaudio"},
                {".rar", "application/x-rar-compressed"},
                {".rpm", "application/x-redhat-package-manager"},
                {".rss", "text/xml"},
                {".run", "application/x-makeself"},
                {".sea", "application/x-sea"},
                {".shtml", "text/html"},
                {".sit", "application/x-stuffit"},
                {".swf", "application/x-shockwave-flash"},
                {".tcl", "application/x-tcl"},
                {".tk", "application/x-tcl"},
                {".txt", "text/plain"},
                {".war", "application/java-archive"},
                {".wbmp", "image/vnd.wap.wbmp"},
                {".wmv", "video/x-ms-wmv"},
                {".xml", "text/xml"},
                {".xpi", "application/x-xpinstall"},
                {".zip", "application/zip"}

                #endregion 
        };

        // IP地址
        private string _ip;
        // 端口
        private int _port;
        // 绑定的服务目录
        private string _rootDirectory;
        // HttpListener
        private HttpListener _listener;
        // 侦听线程
        private Thread _serverThread;
        // 侦听线程状态
        private bool _threadActive;

        #endregion

        #region 属性

        /// <summary>
        /// 绑定的端口
        /// </summary>
        public int Port
        {
            get
            {
                return _port;
            }
        }

        /// <summary>
        /// 绑定的IP
        /// </summary>
        public string Ip
        {
            get => _ip;
        }

        /// <summary>
        /// 服务是否正在运行
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return _threadActive;
            }
        }

        #endregion

        #region 构造

        public SimpleHttpServer(string path, string ip, int port)
        {
            _rootDirectory = path;
            _ip = ip;
            _port = port;
        }

        public SimpleHttpServer(string path)
        {
            // 随机获取一个可用端口
            TcpListener tmpTcp = new TcpListener(IPAddress.Loopback, 0);
            tmpTcp.Start();
            int port = ((IPEndPoint)tmpTcp.LocalEndpoint).Port;
            tmpTcp.Stop();

            _rootDirectory = path;
            _port = port;
            _ip = GetIPString();
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 获取IP地址
        /// </summary>
        /// <returns></returns>
        public string GetIPString()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;

                return endPoint.Address.ToString();
            }
        }


        /// <summary>
        /// 关闭服务
        /// </summary>
        public void Stop(bool force=false)
        {
            _threadActive = false;

            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
            }

            // 停止响应HTTP请求
            if (_serverThread != null)
            {
                if (force)
                {
                    _serverThread.Join();
                }
                _serverThread = null;
            }

            // 关闭Http
            if (_listener != null)
            {
                _listener.Close();
                _listener = null;
            }
        }

        /// <summary>
        /// 开启服务
        /// </summary>
        public void Start()
        {
            if (_serverThread != null)
            {
                Console.WriteLine("服务器已启动.");
                return;
            }

            _serverThread = new Thread(Listen);
            //_serverThread.SetApartmentState(ApartmentState.STA);
            _serverThread.IsBackground = true;
            _serverThread.Start();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 侦听请求
        /// </summary>
        private void Listen()
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("仅支持Windows XP SP2或Windows Server 2003以上版本.");
                return;
            }

            _threadActive = true;

            try
            {
                _listener = new HttpListener();
                //_listener.Prefixes.Add("http://127.0.0.1:" + _port.ToString() + "/");
                //_listener.Prefixes.Add("http://localhost:" + _port.ToString() + "/");
                _listener.Prefixes.Add("http://*:" + _port.ToString() + "/"); // 需要管理员权限运行, 不然会开启失败
                _listener.Start();

            }
            catch (Exception e)
            {
                Console.WriteLine("发生错误: " + e.Message);
                _threadActive = false;

                if (_listener != null && _listener.IsListening)
                {
                    _listener.Stop();
                    _listener.Close();
                } 
                _listener = null;
                return;
            }


            while (_threadActive)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    if (!_threadActive)
                        break;

                    Process(context);
                }
                catch (HttpListenerException e)
                {
                    _threadActive = false;
                    if (e.ErrorCode != 995)
                    {
                        // 程序退出
                        Console.WriteLine("发生错误: " + e.Message);
                    }
                    else
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                catch (Exception ex)
                {
                    _threadActive = false;
                    Console.WriteLine("发生错误: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="context"></param>
        private void Process(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath;
            if (filename != null)
            {
                Console.WriteLine(filename);

                // UrlDecode有空格的文件
                filename = System.Web.HttpUtility.UrlDecode(filename.Substring(1));
            }

            if (string.IsNullOrEmpty(filename))
            {
                foreach (string indexFile in _indexFiles)
                {
                    if (File.Exists(Path.Combine(_rootDirectory, indexFile)))
                    {
                        filename = indexFile;
                        break;
                    }
                }
            }

            filename = Path.Combine(_rootDirectory, filename);

            if (File.Exists(filename))
            {
                try
                {
                    using (Stream stream = new FileStream(filename, FileMode.Open))
                    {
                        string mime;
                        bool knownType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime);
                        context.Response.ContentType = knownType ? mime : "application/octet-stream";
                        context.Response.ContentLength64 = stream.Length;
                        context.Response.SendChunked = stream.Length > 1024 * 16; // 发送大文件

                        //byte[] buffer = new byte[1024 * 16];
                        //int nbytes;
                        //while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        //    context.Response.OutputStream.Write(buffer, 0, nbytes);
                        //input.Close();
                        stream.CopyTo(context.Response.OutputStream);
                        stream.Flush();
                        context.Response.OutputStream.Flush();
                    }
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filename).ToString("r"));
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    Console.WriteLine(ex.Message);
                }

            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                Console.WriteLine("请求的文件不存在.");
            }

            context.Response.OutputStream.Close();
        }


        #endregion
    }
}
