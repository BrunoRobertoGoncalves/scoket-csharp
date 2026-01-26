using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace client
{
    class Program
    {
        static void Main()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1234);

            Thread threadSecundaria = new Thread(() =>
                FileSender.SendDirectory(endpoint, @"C:\Users\Bruno Roberto\Documents\Android_v2\duplicate_type2\"));
            threadSecundaria.Start();

            try
            {
                FileSender.SendDirectory(endpoint, @"C:\Users\Bruno Roberto\Documents\Android_v2\duplicate_type1\");
            }
            finally
            {
                threadSecundaria.Join();
            }
        }
    }
}
