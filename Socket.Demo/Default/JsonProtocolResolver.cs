using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sockets.Interfaces;
using Sockets.Protocols;
using System;

namespace Sockets.Default
{
    public class JsonProtocolResolver : IProtocolResolver
    {
        #region IJsonProtocolResolver实现

        public IProtocolInfo ToEntity(string protocolText)
        {
            IProtocolInfo info = null;

            JObject protocol = JObject.Parse(protocolText.ToLower());
            if (protocol != null)
            {
                switch (Convert.ToInt32(protocol["cmd"].ToString()))
                {
                    case 100:
                        info = Deserialize<S2T_HeartbeatInfo>(protocolText);
                        break;
                    case 101:
                        //info = Deserialize<S2T_ChatInfo>(protocolText);
                        break;
                    case 102:
                        //info = Deserialize<S2T_ConnectInfo>(protocolText);
                        break;
                    case 201:
                        info  = Deserialize<S2T_ConnectInfo>(protocolText);
                        break;
                    case 202:
                        info = Deserialize<S2T_Catchphrase>(protocolText);
                        break;
                    case 203:
                        break;
                    case 205:
                        {
                            info = Deserialize<S2T_QuizCommit>(protocolText);
                        }
                        break;
                }
            }
            else
            {
                Console.WriteLine("未知类型");
            }

            return info;
        }

        public string ToText(IProtocolInfo info)
        {
            return JsonConvert.SerializeObject(info);
        }

        #endregion


        private T Deserialize<T>(string text) where T : IProtocolInfo
        {
            return JsonConvert.DeserializeObject<T>(text);
        }
    }
}
