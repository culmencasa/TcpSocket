using Sockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sockets.Default
{
    /// <summary>
    /// 协议枚举
    /// </summary>
    public class ProtocolTypes : IProtocolTypes
    {
        #region 枚举定义

        public static ProtocolTypes T2S_Active { get; } = new ProtocolTypes(100, "T2S_Active");// 心跳 
        public static ProtocolTypes T2S_Catchphrase { get; } = new ProtocolTypes(101, "T2S_Catchphrase"); // 抢答
        public static ProtocolTypes T2S_CatchphraseResult { get; } = new ProtocolTypes(106, "T2S_CatchphraseResult"); // 抢答结果
        public static ProtocolTypes T2S_Lock { get; } = new ProtocolTypes(102, "T2S_Lock");// 锁屏 
        public static ProtocolTypes T2S_RandomPick { get; } = new ProtocolTypes(103, "T2S_RandomPick");// 随机点名
        public static ProtocolTypes T2S_ScreenQuestion { get; } = new ProtocolTypes(104, "T2S_ScreenQuestion");// 截屏答题 
        public static ProtocolTypes T2S_Quiz { get; } = new ProtocolTypes(105, "T2S_Quiz");// 课堂检测
        public static ProtocolTypes T2S_QuitQuiz { get; } = new ProtocolTypes(107, "T2S_QuitQuiz");// 结束课堂检测

        public static ProtocolTypes S2T_Active { get; } = new ProtocolTypes(200, "S2T_Active"); // 心跳
        public static ProtocolTypes S2T_Connect { get; } = new ProtocolTypes(201, "S2T_Connect"); // 连接
        public static ProtocolTypes S2T_Catchphrase { get; } = new ProtocolTypes(202, "S2T_Catchphrase"); // 抢答
        //public static ProtocolTypes S2T_RandomCommit { get; } = new ProtocolTypes(203, "S2T_RandomCommit");
        public static ProtocolTypes S2T_ScreenQuestion { get; } = new ProtocolTypes(204, "S2T_ScreenQuestion"); // 截屏答题
        public static ProtocolTypes S2T_QuizCommit { get; } = new ProtocolTypes(205, "S2T_QuizCommit"); // 

        // 上面的命令要在下面登记
        private static ProtocolTypes[] _list = new ProtocolTypes[]
        {
            /* 教师命令*/
            T2S_Active,
            T2S_Lock,
            T2S_CatchphraseResult,
            T2S_Quiz,
            T2S_QuitQuiz,
            T2S_RandomPick,
            T2S_ScreenQuestion,

            /* 学生命令 */
            S2T_Active,
            S2T_Connect,
            S2T_Catchphrase,
            S2T_ScreenQuestion, 
            S2T_QuizCommit
        };

        #endregion

        #region 属性

        /// <summary>
        /// 命令集合
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ProtocolTypes> List()
        {
            return _list;
        }

        /// <summary>
        /// 枚举名称 
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// 枚举值
        /// </summary>
        public int Value { get; private set; }

        #endregion

        #region 构造

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="val"></param>
        /// <param name="name"></param>
        private ProtocolTypes(int val, string name)
        {
            Value = val;
            Name = name;
        }

        #endregion

        #region 方法

        /// <summary>
        /// 从名称转换到枚举
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ProtocolTypes FromString(string name)
        {
            return List().Single(r => String.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 从值转换到枚举
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ProtocolTypes FromValue(int value)
        {
            return List().Single(r => r.Value == value);
        }

        #endregion
    }

}