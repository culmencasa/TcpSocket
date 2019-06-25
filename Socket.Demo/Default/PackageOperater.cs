using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sockets.Default
{
    /// <summary>
    /// 处理数据打包的类
    /// </summary>
    public class PackageOperater : IPackageOperater
    {
        #region 字段

        protected byte[] _prefix;
        protected int _partOneLength;
        protected int _partTwoLength;

        protected Queue<byte> _bytesLocker = new Queue<byte>();

        #endregion

        #region 构造

        public PackageOperater(byte[] prefix, int incomingLength)
        {
            _prefix = prefix;
            _partOneLength = prefix.Length;
            _partTwoLength = incomingLength;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 从指定位置读取指定长度的字节
        /// </summary>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        internal byte[] Check(int start, int length)
        {
            lock (_bytesLocker)
            {
                byte[] source = _bytesLocker.ToArray();
                byte[] dest = new byte[length];

                Array.Copy(source, start, dest, 0, length);

                return dest;
            }
        }

        /// <summary>
        /// 从位置0取出指定长度的字节
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        internal byte[] Take(int count)
        {
            byte[] output = null;
            lock (_bytesLocker)
            {
                output = _bytesLocker.Take(count).ToArray();
                for (int i = 0; i < count; i++)
                {
                    _bytesLocker.Dequeue();
                }
            }

            return output;
        }

        /// <summary>
        /// 从位置0删除指定长度的字节
        /// </summary>
        /// <param name="count"></param>
        internal void Throw(int count)
        {
            lock (_bytesLocker)
            { 
                for (int i = 0; i < count; i++)
                {
                    _bytesLocker.Dequeue();
                }
            }
        }

        protected virtual int GetFirsetValidOffset()
        {
            int firstValidOffset = 0;
            lock (_bytesLocker)
            {
                int prefixLength = _partOneLength;

                for (int index = 0; index < _bytesLocker.Count; index++)
                {
                    if (index + prefixLength < _bytesLocker.Count)
                    {
                        byte[] dataToCompare = this.Check(index, prefixLength);
                        if (this.CompareBytes(dataToCompare, _prefix))
                        {
                            firstValidOffset = index;
                            break;
                        }
                    }
                }

                //for (int i = 0; i < received.Length; i++)
                //{
                //    if (received.Length > i + 2)
                //    {
                //        byte b1 = received[i];
                //        byte b2 = received[i + 1];
                //        byte b3 = received[i + 2];

                //        if (b1 == _prefixByte[0] && b2 == _prefixByte[1] && b3 == _prefixByte[2])
                //        {
                //            firstValidOffset = i;
                //            break;
                //        }
                //    }

                //}
            }

            return firstValidOffset;
        }

        protected void GetPackage(List<IPackageInfo> bundle)
        {
            // 查找Prefix位置
            int prefixLength = _partOneLength;
            int bodyLength = _partTwoLength;

            if (_bytesLocker.Count < prefixLength)
            {
                return;
            }
            else if (_bytesLocker.Count < prefixLength + bodyLength)
            {
                return;
            }
            else
            {
                // 如果不从0开始，则丢掉
                int offset = GetFirsetValidOffset();
                if (offset > 0)
                {
                    Throw(offset);
                    GetPackage(bundle);
                }
                else
                {

                    byte[] partOne = this.Check(0, prefixLength);
                    byte[] partTwo = this.Check(prefixLength, bodyLength);

                    int partThreeLength = BitConverter.ToInt32(partTwo, 0);
                    if (_bytesLocker.Count < prefixLength + bodyLength + partThreeLength)
                    {
                        return;
                    }

                    // 取出来
                    this.Take(prefixLength);
                    this.Take(bodyLength);
                    byte[] body = this.Take(partThreeLength);

                    IPackageInfo info = new PackageInfo(partOne, partTwo, body);
                    bundle.Add(info);

                    // 递归
                    GetPackage(bundle);
                }
            }

        }

        protected bool CompareBytes(byte[] data1, byte[] data2)
        {
            if (data1 == null && data2 == null)
            {
                return true;
            }
            if (data1 == null || data2 == null)
            {
                return false;
            }
            if (data1 == data2)
            {
                return true;
            }

            if (data1.Length != data2.Length)
            {
                return false;
            }

            for (int i = 0; i < data1.Length; i++)
            {
                if (data1[i] != data2[i])
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region IPackageOperater成员

        /// <summary>
        /// 装数据
        /// </summary>
        /// <param name="data"></param>
        public void Push(byte[] datas)
        {
            lock (_bytesLocker)
            {
                foreach (byte item in datas)
                {
                    _bytesLocker.Enqueue(item);
                }
            }
        }

        /// <summary>
        /// 取出数据
        /// </summary>
        /// <returns></returns>
        public List<IPackageInfo> HandOver()
        {
            List<IPackageInfo> bundle = new List<IPackageInfo>();

            GetPackage(bundle);
            // 读取长度

            // 读取内容

            /* 1.一段byte[]
             * 2.判断长度是否足够读Prefix
             * 3.不足，继续接收
             * 4.满足，判断长度是否足够读长度
             * 5.不足，继续接收
             * 6.满足，判断长度是否足够读内容
             * 7.不足，继续接收
             * 8.满足，分解成第一段，跳到第1步
             * 
            */

            return bundle;
        }
        
        #endregion

    }
}
