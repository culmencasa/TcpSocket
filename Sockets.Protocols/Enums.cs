using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sockets.Protocols
{
    public enum ProtocolCmds
    {
        T2S_Active = 100,
        T2S_Catchphrase = 101,
        T2S_Lock = 102,
        T2S_RandomPick = 103,
        T2S_ScreenQuestion = 104,
        T2S_Quiz = 105,
        T2S_CatchphraseResult = 106,
        T2S_QuitQuiz = 107,


        S2T_Active = 200,
        S2T_Connect = 201,
        S2T_Catchphrase = 202,
        S2T_RandomCommit = 203,
        S2T_ScreenQuestion = 204,
        S2T_QuizCommit = 205



    }
}
