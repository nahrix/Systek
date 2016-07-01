using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek
{
    public class AreaType
    {
        public const int
            UNIT_TEST = 1,
            NET_LIB = 2,
            AGENT_MESSAGE_HANDLER = 3,
            SERVER_MESSAGE_HANDLER = 4,
            AGENT_INITIALIZATION = 5,
            SERVER_INITIALIZATION = 6,
            SERVER_TCP_LISTENER = 7;
    }
}
