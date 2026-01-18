using System.Net;
using System.Net.Sockets;

namespace client
{
    class Program
    {
        static void Main()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 1234);

            using var sck = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sck.Connect(endpoint);

            foreach (var path in Directory.EnumerateDirectories(
                         "Path"))
            {
                foreach(var paths in Directory.EnumerateDirectories($"{path}"))
                {
                    foreach(var file in Directory.EnumerateFiles($"{paths}", "*.log"))
                    {
                        sck.SendFile(file);
                    }
                }
            }

            sck.Shutdown(SocketShutdown.Both);
        }
    }
}
