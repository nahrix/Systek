using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Net
{
    public interface IConnection
    {
        bool Connected { get; }
        int Timeout { get; set; }
        Exception LastError { get; }

        void Initialize();
        void Close();
        void Send(Message msg);
        List<Message> GetMessages();
    }
}
