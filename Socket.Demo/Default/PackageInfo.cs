using Sockets.Interfaces;
using System;
using System.Text;

namespace Sockets.Default
{
    public class PackageInfo : IPackageInfo
    {
        #region 属性

        public Encoding BodyEncoding
        {
            get;
            set;
        }

        public byte[] Prefix
        {
            get;
            set;
        }

        public byte[] BodyLength
        {
            get;
            set;
        }

        public byte[] Body { get; set; }

        #endregion

        #region 构造

        public PackageInfo()
        {
        }

        public PackageInfo(byte[] prefix, byte[] bodyLength, byte[] body) : this()
        {
            this.Prefix = prefix;
            this.BodyLength = bodyLength;
            this.Body = body;
            this.BodyEncoding = Encoding.UTF8;
        }

        #endregion

        public byte[] ToBytes()
        {
            int prefixLength = this.Prefix.Length;
            int bodyLength = this.BodyLength.Length;
            int bodyContentLength = this.Body.Length;

            byte[] total = new byte[prefixLength + bodyLength + bodyContentLength];

            Buffer.BlockCopy(this.Prefix, 0, total, 0, prefixLength);
            Buffer.BlockCopy(this.BodyLength, 0, total, prefixLength, bodyLength);
            Buffer.BlockCopy(this.Body, 0, total, prefixLength + bodyLength, bodyContentLength);

            return total;
        }

        public override string ToString()
        {
            string WrappedText = null;

            string base64Content = BodyEncoding.GetString(Body);
            WrappedText = ConvertBase64ToCleartext(base64Content, BodyEncoding);

            return WrappedText;
        }


        public void SetBodyFromString(string value)
        {
            string base64Text = ConvertCleartextToBase64(value, this.BodyEncoding);
            this.Body = Encoding.UTF8.GetBytes(base64Text);
        }


        /// <summary>
        /// 明文转Base64
        /// </summary>
        /// <param name="originalText"></param>
        /// <returns></returns>
        private string ConvertCleartextToBase64(string originalText, Encoding encoding)
        {
            byte[] originalUTF8Bytes = encoding.GetBytes(originalText);

            return Convert.ToBase64String(originalUTF8Bytes);
        }

        /// <summary>
        /// Base64转明文
        /// </summary>
        /// <param name="base64Text"></param>
        /// <returns></returns>
        private string ConvertBase64ToCleartext(string base64Text, Encoding encoding)
        {
            byte[] bytes = Convert.FromBase64String(base64Text);

            return encoding.GetString(bytes);
        }

    }
}