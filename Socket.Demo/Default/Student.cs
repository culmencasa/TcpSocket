using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Default
{
    public class Student
    {
        private string _realName;
        private string _avatar;
        private int _sequenceNo;
        private string _classBusinessId;
        private string _className;
        private string _studentBusinessId;
        private string _sex;
        private string _pinYinInitial;
        private bool _showInitial;
        private string _avatarFullUrl;
        private bool _isOnline;

        public string RealName { get => _realName; set => _realName = value; }
        public string Avatar { get => _avatar; set => _avatar = value; }
        public int SequenceNo { get => _sequenceNo; set => _sequenceNo = value; }
        public string ClassBusinessId { get => _classBusinessId; set => _classBusinessId = value; }
        public string ClassName { get => _className; set => _className = value; }
        public string StudentBusinessId { get => _studentBusinessId; set => _studentBusinessId = value; }
        public string Sex { get => _sex; set => _sex = value; }
        public string PinYinInitial { get => _pinYinInitial; set => _pinYinInitial = value; }
        public bool ShowInitial { get => _showInitial; set => _showInitial = value; }
        public string AvatarFullUrl { get => _avatarFullUrl; set => _avatarFullUrl = value; }
        public bool IsOnline { get => _isOnline; set => _isOnline = value; }
    }
}
