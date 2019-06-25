using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Default
{
    /// <summary>
    /// 定义用于维护学生状态的接口
    /// </summary>
    public interface IStudentClientDictionary : IDictionary<Student, ITcpClientProxy>
    {
        /// <summary>
        /// 获取指定学生编号的相应协议
        /// </summary>
        /// <param name="studentBusinessId"></param>
        /// <returns></returns>
        IProtocolInfo this[string studentBusinessId] { get; set; }

        bool IsClientValid(string studentBusinessId);
    }
}
