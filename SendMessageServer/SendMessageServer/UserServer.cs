using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SendMessageServer
{
    public class UserServer
    {
        public int Nomer { set; get; }
        public string Name { set; get; }
        public object UserSocket { set; get; }
    }
}
