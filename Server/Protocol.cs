using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    internal static class Protocol
    {
        public static async Task<byte[]> ReadExactAsync(NetworkStream stream, int length)
        {
            byte[] buffer = new byte[length];
            int offset = 0;

            while (offset < length)
            {
                int read = await stream.ReadAsync(buffer, offset, length - offset);
                if (read == 0) throw new IOException("Conexão caiu durante leitura.");
                offset += read;
            }

            return buffer;
        }

        public static async Task<long> ReadInt64BEAsync(NetworkStream stream)
        {
            byte[] buf = await ReadExactAsync(stream, 8);
            if (BitConverter.IsLittleEndian) Array.Reverse(buf);
            return BitConverter.ToInt64(buf, 0);
        }

        public static async Task<ushort> ReadUInt16BEAsync(NetworkStream stream)
        {
            byte[] buf = await ReadExactAsync(stream, 2);
            if (BitConverter.IsLittleEndian) Array.Reverse(buf);
            return BitConverter.ToUInt16(buf, 0);
        }

        internal sealed class LimitedReadStream : Stream
        {
            private readonly Stream _inner;
            private long _remaining;

            public LimitedReadStream(Stream inner, long limitBytes)
            {
                _inner = inner;
                _remaining = limitBytes;
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_remaining <= 0) return 0;
                int toRead = (int)Math.Min(count, _remaining);
                int read = _inner.Read(buffer, offset, toRead);
                _remaining -= read;
                return read;
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (_remaining <= 0) return 0;
                int toRead = (int)Math.Min(count, _remaining);
                int read = await _inner.ReadAsync(buffer.AsMemory(offset, toRead), cancellationToken);
                _remaining -= read;
                return read;
            }

            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
