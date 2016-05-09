using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Net
{
    [Serializable]
    enum MessageType
    {
        CONTROL = 1,
        COMMAND = 2
    };

    [Serializable]
    struct Message
    {
        MessageType Type;
        int Sequence;
        String Command;
        List<String> Parameters;
    }
}
