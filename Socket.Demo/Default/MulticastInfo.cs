using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sockets.Default
{
    /// <summary>
    /// 组播信息体
    /// </summary>
    public class MulticastInfo : IMulticastInfo
    {
        protected string separator = ":";

        public string IPAddress
        {
            get;
            set;
        }

        public int Port
        {
            get;
            set;
        }

        public string ClassId
        {
            get;
            set;
        }

        public string TextbookId
        {
            get;
            set;
        }

        public string ChapterId
        {
            get;
            set;
        }


        public string HttpPort
        {
            get;
            set;
        }

        public MulticastInfo()
        {
        }

        public override string ToString()
        {
            string content = string.Join(separator,
                new object[] { IPAddress, Port, ClassId, TextbookId, ChapterId });
            return content;
        }

        public byte[] ToBtyes()
        {
            byte[] dataBytes = Encoding.Default.GetBytes(this.ToString());
            return dataBytes;
        }
    }
}
