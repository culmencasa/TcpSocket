using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sockets.Interfaces
{
    public interface ITcpServer
    {
        bool Send(ITcpClientProxy target, IPackageInfo content);
    }
}
