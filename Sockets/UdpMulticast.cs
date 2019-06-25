using System;
using System.Collections.Generic;
using System.Linq;
using System.Text; 
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using Sockets.Interfaces;

namespace Sockets
{
    /// <summary>
    /// 组播发起者
    /// </summary>
    public class UdpMulticast
    {
        #region 字段

        UdpClient _originator;
        IPEndPoint _targetEndPoint;
        IPEndPoint _localEndPoint;

        EZLogger _logger;
        Timer _periodThread;

        #endregion

        #region 属性

        public Settings Setting { get; set; }
        
        #endregion

        #region 构造

        public UdpMulticast(Settings setting)
        {
            this.Setting = setting;  

            StartLog();

            try
            {
                _targetEndPoint = new IPEndPoint(IPAddress.Parse(setting.TargetIP), setting.TargetPort);
                _localEndPoint = new IPEndPoint(IPAddress.Parse(setting.LocalIP), setting.LocalPort);
                // 绑定并监听发起人
                _originator = new UdpClient(_localEndPoint);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.StackTrace);
                throw ex;
            }
        }




        #endregion

        #region 公共方法

        /// <summary>
        /// 开启一个定时器线程发送消息
        /// </summary>
        public void Start(IMulticastInfo content)
        {
            _periodThread = new Timer((o)=> {
                try
                {
                    byte[] dgram = content.ToBtyes();
                    _originator.Send(dgram, dgram.Length, _targetEndPoint);

                    Console.WriteLine("Broadcast " + Encoding.UTF8.GetString(dgram));
                }
                catch (Exception ex)
                {
                    _logger.Error("发送组播时错误: " + ex.StackTrace);
                } 
            }, content, 2000, Setting.Period);
        }

        public void Stop()
        {
            if (_periodThread != null)
            {
                _periodThread.Dispose();
                _periodThread = null;
            }

            if (_originator != null)
            {
                _originator.Close();
            }
        }

        #endregion

        #region 私有方法
        
        /// <summary>
        /// 启动日志
        /// </summary>
        protected void StartLog()
        {
            // 启动日志
            _logger = new EZLogger(
                @"Logs\IpMulticast_" + DateTime.Now.ToString("yyyyMMdd") + ".log",
                true,
                (uint)EZLogger.Level.All);
            _logger.Start();

        }

        #endregion



        /// <summary>
        /// 组播设置
        /// </summary>
        public class Settings
        {
            /// <summary>
            /// 组播目标IP
            /// </summary>
            public string TargetIP { get; set; }

            /// <summary>
            /// 组播端口
            /// </summary>
            public int TargetPort { get; set; }

            /// <summary>
            /// 本地绑定IP
            /// </summary>
            public string LocalIP { get; set; }

            /// <summary>
            ///  使用任意本地端口作为侦听端口
            /// </summary>
            public int LocalPort { get; set; } = 5566;

            /// <summary>
            /// 发送间隔
            /// </summary>
            public int Period { get; set; } = 10000;
        }
    }
}
