using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Common
{

    public static class Network
    {
        /// <summary>
        /// 获取本地IP地址信息
        /// </summary>
        /// <returns></returns>
        public static IList<NetworkInfo> ShowNetworkInterfaceMessage()
        {
            List<NetworkInfo> returnList = new List<NetworkInfo>();


            //获取本地计算机上网络接口的对象
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                //有线与无线，且处于在线状态为筛选条件
                if (adapter.OperationalStatus != OperationalStatus.Up)
                    continue;

                string typeName = adapter.NetworkInterfaceType.ToString();
                if (typeName.Equals("Ethernet") || typeName.Equals("Wireless80211"))
                {
                    // 跳过VMware虚拟网卡
                    if (adapter.Name.Contains("VMware"))
                        continue;

                    //获取以太网卡网络接口信息
                    IPInterfaceProperties ipProps = adapter.GetIPProperties();
                    //遍历单播地址集
                    foreach (UnicastIPAddressInformation uni in ipProps.UnicastAddresses)
                    {
                        //InterNetwork    IPV4地址      InterNetworkV6        IPV6地址
                        //Max            MAX 位址
                        //判断是否为ipv4
                        if (uni.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            NetworkInfo info = new NetworkInfo();
                            info.IpAddress = uni.Address.ToString();
                            info.Name = adapter.Name;
                            info.NetworkType = typeName;

                            returnList.Add(info);
                        }
                    }

                }

            }

            return returnList;
        }



        /// <summary>
        /// 获取正在使用的IP
        /// </summary>
        /// <returns></returns>
        public static NetworkInfo GetActiveIPv4()
        {
            NetworkInfo info = new NetworkInfo();

            // 连百度
            UdpClient u = new UdpClient("180.97.33.108", 1);
            //UdpClient u = new UdpClient(System.Net.Dns.GetHostName(), 1);
            IPAddress localAddr = (u.Client.LocalEndPoint as IPEndPoint).Address;

            //获取本地计算机上网络接口的对象
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                //有线与无线，且处于在线状态为筛选条件
                if (adapter.OperationalStatus != OperationalStatus.Up)
                    continue;

                string typeName = adapter.NetworkInterfaceType.ToString();
                if (typeName.Equals("Ethernet") || typeName.Equals("Wireless80211"))
                {
                    // 跳过VMware虚拟网卡
                    if (adapter.Name.Contains("VMware"))
                        continue;

                    //获取以太网卡网络接口信息
                    IPInterfaceProperties ipProps = adapter.GetIPProperties();
                    //遍历单播地址集
                    foreach (UnicastIPAddressInformation uni in ipProps.UnicastAddresses)
                    {
                        //InterNetwork    IPV4地址      InterNetworkV6        IPV6地址       Max            MAX 位址
                        //判断是否为ipv4
                        if (uni.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (uni.Address.ToString() == localAddr.ToString())
                            {
                                info.IpAddress = uni.Address.ToString();
                                info.NetworkType = typeName;
                                info.Name = adapter.Name;
                                //if (typeName.Equals("Ethernet"))
                                //{
                                //    info.Name = adapter.Name;
                                //}
                                //else
                                //{
                                //    info.Name = GetName();
                                //}

                                return info;
                            }
                        }
                    }

                }

            }

            return info;
        }


        /// <summary>
        /// 获取IP地址
        /// </summary>
        /// <returns></returns>
        public static string GetIPString()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;

                return endPoint.Address.ToString();
            }
        }


        public static string GetName()
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "netsh.exe",
                    Arguments = "wlan show interfaces",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            var line = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                             .FirstOrDefault(l => l.Contains("SSID") && !l.Contains("BSSID"));
            if (line == null)
            {
                return string.Empty;
            }
            var ssid = line.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1].TrimStart();
            return ssid;
        }


        #region 端口处理

        /// <summary>
        /// 检查指定端口是否已用
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool PortIsAvailable(int port)
        {
            bool isAvailable = true;

            IList portUsed = PortIsUsed();

            foreach (int p in portUsed)
            {
                if (p == port)
                {
                    isAvailable = false; break;
                }
            }

            return isAvailable;
        }
        /// <summary>
        /// 获取操作系统已用的端口号
        /// </summary>
        /// <returns></returns>
        public static IList PortIsUsed()
        {
            //获取本地计算机的网络连接和通信统计数据的信息
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            //返回本地计算机上的所有Tcp监听程序
            IPEndPoint[] ipsTCP = ipGlobalProperties.GetActiveTcpListeners();

            //返回本地计算机上的所有UDP监听程序
            IPEndPoint[] ipsUDP = ipGlobalProperties.GetActiveUdpListeners();

            //返回本地计算机上的Internet协议版本4(IPV4 传输控制协议(TCP)连接的信息。
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            IList allPorts = new ArrayList();
            foreach (IPEndPoint ep in ipsTCP) allPorts.Add(ep.Port);
            foreach (IPEndPoint ep in ipsUDP) allPorts.Add(ep.Port);
            foreach (TcpConnectionInformation conn in tcpConnInfoArray) allPorts.Add(conn.LocalEndPoint.Port);

            return allPorts;
        }
        /// <summary>
        /// 获取第一个可用的端口号
        /// </summary>
        /// <returns></returns>
        public static int GetFirstAvailablePort()
        {
            int MAX_PORT = 6000; //系统tcp/udp端口数最大是65535            
            int BEGIN_PORT = 5000;//从这个端口开始检测

            for (int i = BEGIN_PORT; i < MAX_PORT; i++)
            {
                if (PortIsAvailable(i)) return i;
            }

            return -1;
        }

        #endregion
    }



}
