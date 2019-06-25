using Sockets.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Sockets.Default
{
    public class ClientDictionary : ConcurrentDictionary<Student, ITcpClientProxy>, IStudentClientDictionary
    {
        public IProtocolInfo this[string studentBusinessId]
        {
            get{
                return null;
            }
            set
            {

            }
        }


        #region 构造

        public ClientDictionary()
        { }
        public ClientDictionary(IEnumerable<Student> students) : base()
        {
            foreach (var item in students)
            {
                Add(item, null);
            }
        }

        #endregion

        /// <summary>
        /// 班级Id, 作为标识
        /// </summary>
        public string ClassId { get; set; }


        #region 公开方法

        /// <summary>
        /// 添加学生设备
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Add(Student key, ITcpClientProxy value)
        {
            if (key.StudentBusinessId == null)
                throw new ArgumentNullException("学生编号不能为空。");

            foreach (Student item in this.Keys){
                if (item.StudentBusinessId == key.StudentBusinessId)
                    return false;
            }

            base.TryAdd(key, value);

            return true;
        }

        /// <summary>
        /// 删除学生设备
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(Student key)
        {
            Student target = null;
            foreach (Student item in this.Keys)
            {
                if (item.StudentBusinessId == key.StudentBusinessId)
                {
                    target = item;
                    break;
                }
            }

            return base.TryRemove(target, out ITcpClientProxy value);
        }


        public void Reset()
        {
            Student[] keys = this.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                if (this[keys[i]] != null)
                {
                    //this[keys[i]].Dispose();
                    this[keys[i]] = null;
                }
            }
        }

        /// <summary>
        /// 客户端是否有效
        /// </summary>
        /// <param name="studentBusinessId"></param>
        /// <returns></returns>
        public bool IsClientValid(string studentBusinessId)
        {
            bool isValueValid = false;
            KeyValuePair<Student, ITcpClientProxy> kvp = this.FirstOrDefault(w => w.Key.StudentBusinessId == studentBusinessId);
            if (kvp.Value != null)
            {
                isValueValid = true;
            }

            return isValueValid;
        }


        public Student GetStudent(string studentBusinessId)
        {
            try
            {
                KeyValuePair<Student, ITcpClientProxy> kvp = this.FirstOrDefault(w => w.Key.StudentBusinessId == studentBusinessId);

                return kvp.Key;
            }
            catch
            {
                return null;
            }
        }

        public ITcpClientProxy GetDevice(string studentBusinessId)
        {
            try
            {
                KeyValuePair<Student, ITcpClientProxy> kvp = this.FirstOrDefault(w => w.Key.StudentBusinessId == studentBusinessId);
                return kvp.Value;
            }
            catch
            {
                return null;
            }
        }

        #endregion

    }
}
