using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sockets.Interfaces
{

    /// <summary>
    /// 封包的设置
    /// </summary>
    public interface IPackageSettings
    {
        /// <summary>
        /// 前缀
        /// </summary>
        string PrefixString { get; set; }

        /// <summary>
        /// 表示最大长度
        /// </summary>
        string LengthString { get; set; }

        /// <summary>
        /// 编码
        /// </summary>
        Encoding Encoding { get; set; }

        /// <summary>
        /// 获取或设置用于接收消息的缓冲区大小
        /// </summary>
        int BufferSize { get; set; }

        int MaxConnectionNumber { get; set; }
    }
}
