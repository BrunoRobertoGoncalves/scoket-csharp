using System.Net;
using System.Net.Sockets;

namespace client
{
    class Program
    {
        static void Main()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1234);

            Thread threadSecundaria = new Thread(SocketSendSecondaryThread);
            threadSecundaria.Start();

            try
            {
                using var sck = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sck.Connect(endpoint);

                foreach (var path in Directory.EnumerateDirectories(
                             @"C:\Users\Bruno Roberto\Documents\Android_v2\duplicate_type1\"))
                {

                    foreach (var file in Directory.EnumerateFiles(path, "*.log"))
                    {
                        sck.SendFile(file);
                    }

                }

                sck.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                threadSecundaria.Join();
            }
        }

        private static void SocketSendSecondaryThread()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1234);

            using var sck = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sck.Connect(endpoint);

            foreach (var path in Directory.EnumerateDirectories(
                         @"C:\Users\Bruno Roberto\Documents\Android_v2\duplicate_type2\"))
            {

                foreach (var file in Directory.EnumerateFiles(path, "*.log"))
                {
                    sck.SendFile(file);
                }

            }

            sck.Shutdown(SocketShutdown.Both);
        }
    }
}
