using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Interfaces
{
    /// <summary>
    /// Json协议
    /// </summary>
    public interface IProtocolResolver
    {
        IProtocolInfo ToEntity(string protocolText);

        string ToText(IProtocolInfo entity);
    }
}
