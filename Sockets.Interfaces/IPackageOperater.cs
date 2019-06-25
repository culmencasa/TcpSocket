using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sockets.Interfaces
{
    /// <summary>
    /// 封包解包操作者接口
    /// </summary>
    public interface IPackageOperater
    {
        /// <summary>
        /// 向包裹中放数据
        /// </summary>
        /// <param name="data"></param>
        void Push(byte[] data);
               

        /// <summary>
        /// 移交包裹(零个或多个)
        /// </summary>
        /// <returns></returns>
        List<IPackageInfo> HandOver();
    }
}
 