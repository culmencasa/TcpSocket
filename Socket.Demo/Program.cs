using Sockets.Common;
using Sockets.Default;
using Sockets.Interfaces;
using Sockets.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerManager.StartTcpServer();

            if (ServerManager.IsOpened)
            {
                Console.WriteLine($"TCP服务状态:开启 - {ServerManager.ServerEndPoint.Address}:{ServerManager.ServerEndPoint.Port}");

                ServerManager.Events.ClientConnected += Events_ClientConnected;
                ServerManager.Events.MessageReceived += Events_MessageReceived;
                ServerManager.Events.ClientDisconnected += Events_ClientDisconnected;
                ServerManager.Events.MessageSend += Events_MessageSend;


                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("连接失败");
                Console.ReadLine();
            }
        }


        #region TCP服务事件处理


        // 客户端连接
        private static void Events_ClientConnected(object sender, ClientStateEventArgs e)
        {
            e.ClientProxy.ValidatingHandler += (a) => { return true; };


            //logger.Info(string.Format("客户端 {0} 连接成功 ", e.ClientEndPoint));
        }

        // 接收到客户端消息 
        private static void Events_MessageReceived(object sender, ClientStateEventArgs e)
        {
            ServerManager.HandleReceivedPackages(e, (context, protocol) =>
            {
                ProtocolTypes type = ProtocolTypes.FromValue(protocol.cmd);

                // 收到学生连接请求
                if (type == ProtocolTypes.S2T_Connect)
                {
                    // 验证是否当前课堂的学生
                    var valid = DoFirstValidation(context, protocol);



                    // 保持连接, 发送锁屏命令
                    if (valid)
                    {
                        T2S_LockScreenInfo proc = new T2S_LockScreenInfo();
                        proc.msg.state = 1;
                        ServerManager.Send(proc);
                    }
                }

                // debug
                //var package = ServerManager.DefaultPackageDealer.Seal(protocol);
                //logger.Debug("收到消息 :" + protocol.ToString());
                //logger.Debug(package.ToString());


            });
        }

        private static void Events_MessageSend(object sender, ClientStateEventArgs e)
        {
            Console.WriteLine("发送了一条消息 " + e.LastPackages.Last().ToString());
        }

        private static void Events_ClientDisconnected(object sender, ClientStateEventArgs e)
        {
            Console.WriteLine(e.ClientEndPoint.ToString() + "断线了");

            // 更新学生在线状况
            var device = ServerManager.StudentDevices;
            if (device != null)
            {
                var student = device.Keys.FirstOrDefault(p => p.StudentBusinessId == e.ClientProxy.BusinessID);
                if (student != null)
                {
                    student.IsOnline = false;
                }
            }
        }



        /// <summary>
        /// 验证学生
        /// </summary>
        /// <param name="e"></param>
        /// <param name="info"></param>
        private static bool DoFirstValidation(ClientStateEventArgs e, IProtocolInfo info)
        {
            S2T_ConnectInfo connectInfo = info as S2T_ConnectInfo;

            // 新的客户端
            var ThisClient = e.ClientProxy;
            // 新的学生编号
            var ThisBusinessId = connectInfo.msg.sId;
            // 全局的学生清单 
            var StudentDevices = ServerManager.StudentDevices;
            lock (StudentDevices)
            {
                // 查询学生清单中是否存在对应编号的学生
                Student key = StudentDevices.Keys.FirstOrDefault(p => p.StudentBusinessId == ThisBusinessId);
                if (key != null)
                {
                    var ValueClient = StudentDevices[key];
                    if (ValueClient == null)
                    {
                        // 如果不存在，则关联学生对象和客户端对象
                        StudentDevices[key] = ThisClient;
                        ThisClient.BusinessID = key.StudentBusinessId; // 关联业务ID，以便下次作比较

                        // 2019-04-16 mzc
                        //e.BusinessId = ThisBusinessId;

                        key.IsOnline = true;

                        Console.WriteLine($"学生{key.RealName}连接上了");

                    }
                    else
                    {
                        // 如果已存在，判断客户端列表里是否有相同ID的连接（比较业务ID，如果相同，则断开之前的连接）
                        if (ValueClient.BusinessID.ToDefaultString() == ThisBusinessId)
                        {
                            var lastClient = StudentDevices[key];

                            //todo: 改为发命令 
                            //ServerManager.Recycle(lastClient.ReceiveContext);

                            Console.WriteLine("同一学生重复连接，回收之前的一个连接，并保留当前连接。");
                        }

                        StudentDevices[key] = ThisClient;
                        ThisClient.BusinessID = key.StudentBusinessId;

                        key.IsOnline = true;


                        // 2019-04-16 mzc
                        //e.BusinessId = ThisBusinessId;
                    }

                    return true;
                }
                else
                {
                    // 如果连接上来的不是该班级的学生，则断开连接
                    ServerManager.Recycle(e.ClientSocketContext);

                    return false;
                }
            }
        }


        #endregion

    }
}
