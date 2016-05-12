using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Net
{
    public interface IConnection
    {
        bool Active { get; }

        void Initialize();
        void Send(Message msg);
        List<Message> GetMessages();
    }
}
