using Newtonsoft.Json.Linq;
using Sockets.Default;
using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sockets.Default
{
    public class PackageDealer 
    {
        IProtocolResolver DefaultResolver { get; set; } = new JsonProtocolResolver();


        #region IPackageDealer成员

        /// <summary>
        /// 相关设置
        /// </summary>
        public IPackageSettings PackageSetting
        {
            get;
            set;
        }


        /// <summary>
        /// 打开包裹得到协议
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public IProtocolInfo Open(IPackageInfo package)
        {
            IProtocolInfo result = null;

            string WrappedText = package.ToString();
            
            //test
            Console.WriteLine("WrappedText: {0}", WrappedText);

            result =  DefaultResolver.ToEntity(WrappedText);

            return result;
        }

        /// <summary>
        /// 将协议封装成包裹
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public IPackageInfo Seal(IProtocolInfo protocol)
        {
            PackageInfo info = new PackageInfo();
            info.Prefix = Encoding.UTF8.GetBytes(PackageSetting.PrefixString);
            info.BodyEncoding = PackageSetting.Encoding;
            
            string protocolPlainText = DefaultResolver.ToText(protocol);
            info.SetBodyFromString(protocolPlainText); 

            info.BodyLength = BitConverter.GetBytes(Convert.ToInt32(info.Body.Length));

            return info;
        }

        #endregion

         
    }
}
