using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace server
{
    class Program
    {
        static async Task Main()
        {
            var listener = new TcpListener(IPAddress.Loopback, 1234);
            listener.Start();

            System.Console.WriteLine("Server started and listening on port 1234...");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => LogIngestor.HandleClientAsync(client));
            }
        }
    }
}
