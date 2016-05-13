using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Net
{
    [Serializable]
    public enum MessageType
    {
        CONTROL = 1,
        COMMAND = 2
    };

    [Serializable]
    public struct Message
    {
        public MessageType Type;
        public int Sequence;
        public String Command;
        public List<String> Parameters;
    }
}
