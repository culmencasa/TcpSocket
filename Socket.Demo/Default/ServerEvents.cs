using Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Default
{
    public class ServerEvents
    {

        #region 事件
        
        public event Action<object, EventHandler<ClientStateEventArgs>> ClientConnectedEventChanged;
        public event Action<object, EventHandler<ClientStateEventArgs>> ClientDisconnectedEventChanged;
        public event Action<object, EventHandler<ClientStateEventArgs>> MessageSendEventChanged;
        public event Action<object, EventHandler<ClientStateEventArgs>> MessageReceivedEventChanged;
        
        #endregion

        public class ClientStateEventHandlerList : List<EventHandler<ClientStateEventArgs>> {
             
        }

        public ClientStateEventHandlerList ConnectedEventList;
        public ClientStateEventHandlerList DisconnectedEventList;
        public ClientStateEventHandlerList SentEventList;
        public ClientStateEventHandlerList ReceivedEventList;

        public void Clear()
        {
            ConnectedEventList.Clear();
            DisconnectedEventList.Clear();
            SentEventList.Clear();
            ReceivedEventList.Clear();
        }

        #region 构造

        public ServerEvents()
        {
            ConnectedEventList = new ClientStateEventHandlerList();
            DisconnectedEventList = new ClientStateEventHandlerList();
            SentEventList = new ClientStateEventHandlerList();
            ReceivedEventList = new ClientStateEventHandlerList();
        }

        #endregion


        public event EventHandler<ClientStateEventArgs> ClientConnected
        {
            add
            {
                if (!ConnectedEventList.Contains(value))
                {
                    ConnectedEventList.Add(value);
                    ClientConnectedEventChanged?.Invoke(value.Target, value);
                }
            }
            remove
            {
                if (ConnectedEventList.Contains(value))
                {
                    ConnectedEventList.Remove(value);
                    ClientConnectedEventChanged?.Invoke(value.Target, value);
                }
            }
        }

        public event EventHandler<ClientStateEventArgs> ClientDisconnected
        {
            add
            {
                if (!DisconnectedEventList.Contains(value))
                {
                    DisconnectedEventList.Add(value);
                    ClientDisconnectedEventChanged?.Invoke(value.Target, value);
                }
            }
            remove
            {
                if (DisconnectedEventList.Contains(value))
                {
                    DisconnectedEventList.Remove(value);
                    ClientDisconnectedEventChanged?.Invoke(value.Target, value);
                }
            }
        }

        public event EventHandler<ClientStateEventArgs> MessageSend
        {
            add
            {
                if (!SentEventList.Contains(value))
                {
                    SentEventList.Add(value);
                    MessageSendEventChanged?.Invoke(value.Target, value);
                }
            }
            remove
            {
                if (SentEventList.Contains(value))
                {
                    SentEventList.Remove(value);
                    MessageSendEventChanged?.Invoke(value.Target, value);
                }
            }
        }

        public event EventHandler<ClientStateEventArgs> MessageReceived
        {
            add
            {
                if (!ReceivedEventList.Contains(value))
                {
                    ReceivedEventList.Add(value);
                    MessageReceivedEventChanged?.Invoke(value.Target, value);
                }
            }
            remove
            {
                if (ReceivedEventList.Contains(value))
                {
                    ReceivedEventList.Remove(value);
                    MessageReceivedEventChanged?.Invoke(value.Target, value);
                }
            }
        }       

         
    }
}
