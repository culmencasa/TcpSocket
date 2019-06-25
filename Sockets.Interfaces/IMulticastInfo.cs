namespace Sockets.Interfaces
{
    public interface IMulticastInfo
    {
        /// <summary>
        /// 班级Id
        /// </summary>
        string ClassId { get; set; }

        /// <summary>
        /// Http端口(HTTP)
        /// </summary>
        string HttpPort { get; set; }
        /// <summary>
        /// IP地址(TCP)
        /// </summary>
        string IPAddress { get; set; }
        /// <summary>
        /// IP端口(TCP)
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// 科目Id
        /// </summary>
        string TextbookId { get; set; }

        /// <summary>
        /// 转换成byte数组
        /// </summary>
        /// <returns></returns>
        byte[] ToBtyes();

        /// <summary>
        /// 转换成字符串
        /// </summary>
        /// <returns></returns>
        string ToString();
    }
}