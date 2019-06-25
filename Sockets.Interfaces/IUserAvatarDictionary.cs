using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Sockets.Interfaces
{
    public interface IUserAvatarDictionary : IDictionary<String, ISerializable>
    {
    }
}
