using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sockets
{

    /// <summary>
    /// 创建一个大型缓冲区，该缓冲区可以进行分割并指定给 SocketAsyncEventArgs 对象以便用在每个套接字 I/O 操作中。 
    /// 这样可以很方便地重用缓冲区，并防止堆内存碎片化。
    /// 
    /// BufferManager暴露的方法是非线程安全的
    /// </summary>
    internal class BufferManager
    {
        int totalBytesInBufferBlock;       
        byte[] bufferBlock;                
        Stack<int> freeIndexPool;     
        int currentIndex;
        int bufferBytesAllocatedForEachSaea;

        public BufferManager(int totalBytes, int bufferSize)
        {
            totalBytesInBufferBlock = totalBytes;
            currentIndex = 0;
            bufferBytesAllocatedForEachSaea = bufferSize;
            freeIndexPool = new Stack<int>();
        }

        /// <summary>
        /// 分配缓冲区
        /// </summary>
        internal void InitBuffer()
        {
            // create one big large buffer and divide that 
            // out to each SocketAsyncEventArg object
            bufferBlock = new byte[totalBytesInBufferBlock];
        }

        /// <summary>
        /// Assigns a buffer from the buffer pool to the specified SocketAsyncEventArgs object
        /// </summary>
        /// <param name="args"></param>
        /// <returns>true if the buffer was successfully set, else false</returns>
        internal bool SetBuffer(SocketAsyncEventArgs args)
        {

            if (freeIndexPool.Count > 0)
            {
                //This if-statement is only true if you have called the FreeBuffer
                //method previously, which would put an offset for a buffer space 
                //back into this stack.
                args.SetBuffer(bufferBlock, freeIndexPool.Pop(), bufferBytesAllocatedForEachSaea);
            }
            else
            {
                //Inside this else-statement is the code that is used to set the 
                //buffer for each SAEA object when the pool of SAEA objects is built
                //in the Init method.
                if ((totalBytesInBufferBlock - bufferBytesAllocatedForEachSaea) < currentIndex)
                {
                    return false;
                }
                args.SetBuffer(bufferBlock, currentIndex, bufferBytesAllocatedForEachSaea);
                currentIndex += bufferBytesAllocatedForEachSaea;
            }
            return true;
        }


        // Removes the buffer from a SocketAsyncEventArg object.   This frees the
        // buffer back to the buffer pool. Try NOT to use the FreeBuffer method,
        // unless you need to destroy the SAEA object, or maybe in the case
        // of some exception handling. Instead, on the server
        // keep the same buffer space assigned to one SAEA object for the duration of
        // this app's running.
        internal void FreeBuffer(SocketAsyncEventArgs args)
        {
            freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }

    }
}
