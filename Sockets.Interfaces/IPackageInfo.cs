using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sockets.Interfaces
{
    /// <summary>
    /// 封包接口
    /// </summary>
    public interface IPackageInfo
    {
        /// <summary>
        /// 编码
        /// </summary>
        Encoding BodyEncoding { get; set; }

        /// <summary>
        /// 
        /// </summary>
        byte[] Prefix { get; set; }

        /// <summary>
        /// Body长度
        /// </summary>
        byte[] BodyLength { get; set; }

        /// <summary>
        /// Body内容
        /// </summary>
        byte[] Body { get; set; }

        /// <summary>
        /// 输出成byte数组
        /// </summary>
        /// <returns></returns>
        byte[] ToBytes();
    }
}
