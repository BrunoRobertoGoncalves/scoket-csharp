using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace client
{
    internal static class FileSender
    {
        public static void SendDirectory(IPEndPoint endpoint, string rootDir)
        {
            using var sck = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sck.Connect(endpoint);

            foreach (var dir in Directory.EnumerateDirectories(rootDir))
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*.log"))
                {
                    SendFileInChunks(sck, file);
                }
            }

            SendEndOfFiles(sck);
            sck.Shutdown(SocketShutdown.Both);
        }

        private static void SendFileInChunks(Socket sck, string filePath, int chunkSize = 64 * 1024)
        {
            var fi = new FileInfo(filePath);
            long fileSize = fi.Length;
            string fileName = fi.Name;

            SendInt64BE(sck, fileSize);
            SendUInt16BE(sck, (ushort)Encoding.UTF8.GetByteCount(fileName));
            SendAll(sck, Encoding.UTF8.GetBytes(fileName));

            byte[] buffer = new byte[chunkSize];
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            while (true)
            {
                int read = fs.Read(buffer, 0, buffer.Length);
                if (read <= 0) break;
                SendAll(sck, buffer, read);
            }
        }

        private static void SendEndOfFiles(Socket sck) => SendInt64BE(sck, -1);

        private static void SendAll(Socket sck, byte[] data) => SendAll(sck, data, data.Length);

        private static void SendAll(Socket sck, byte[] data, int count)
        {
            int sentTotal = 0;
            while (sentTotal < count)
            {
                int sent = sck.Send(data, sentTotal, count - sentTotal, SocketFlags.None);
                if (sent <= 0) throw new IOException("Socket fechou durante envio.");
                sentTotal += sent;
            }
        }

        private static void SendInt64BE(Socket sck, long value)
        {
            byte[] buf = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(buf);
            SendAll(sck, buf);
        }

        private static void SendUInt16BE(Socket sck, ushort value)
        {
            byte[] buf = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(buf);
            SendAll(sck, buf);
        }
    }
}
