using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;

namespace server
{
    class Program
    {
        const string ConnStr =
            @"Server=(localdb)\MSSQLLocalDB;Database=TRANSFER_DB;Integrated Security=true;TrustServerCertificate=True;";

        const int BatchSize = 10_000;

        static async Task Main()
        {
            var listener = new TcpListener(IPAddress.Loopback, 1234);
            listener.Start();

            Console.WriteLine("Server started and listening on port 1234...");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();

                _ = Task.Run(() => HandleClient(client));
            }
        }

        private static void HandleClient(TcpClient client)
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Client connected. Starting to process data...");

            using var reader = new StreamReader(client.GetStream(), Encoding.UTF8, false, 65536);

            using var con = new SqlConnection(ConnStr);
            con.Open();

            var dt = CreateTable();
            using var bulk = CreateBulkCopy(con);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (!TryAddRow(line, dt)) continue;

                if (dt.Rows.Count >= BatchSize)
                    Flush(dt, bulk);
            }

            if (dt.Rows.Count > 0)
                Flush(dt, bulk);
        }

        static bool TryAddRow(string line, DataTable dt)
        {
            var parts = line.Split(' ', 7, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6) return false;

            if (!int.TryParse(parts[2], out var pid)) return false;
            if (!int.TryParse(parts[3], out var tid)) return false;

            var component = parts[5];
            if (component.Length > 0 && component[^1] == ':')
                component = component[..^1];

            var content = parts.Length == 7 ? parts[6] : string.Empty;

            var logDate = $"{parts[0]} {parts[1]}";

            dt.Rows.Add(logDate, pid, tid, parts[4], component, content);
            return true;
        }

        static void Flush(DataTable dt, SqlBulkCopy bulk)
        {
            bulk.WriteToServer(dt);
            dt.Clear();
        }

        static DataTable CreateTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("LogDate", typeof(string));
            dt.Columns.Add("Pid", typeof(int));
            dt.Columns.Add("Tid", typeof(int));
            dt.Columns.Add("Level", typeof(string));
            dt.Columns.Add("Component", typeof(string));
            dt.Columns.Add("Content", typeof(string));
            return dt;
        }

        static SqlBulkCopy CreateBulkCopy(SqlConnection con)
        {
            var bulk = new SqlBulkCopy(
                con,
                SqlBulkCopyOptions.TableLock,
                null)
            {
                DestinationTableName = "dbo.AndroidLogs",
                BatchSize = BatchSize
            };

            bulk.ColumnMappings.Add("LogDate", "LogDate");
            bulk.ColumnMappings.Add("Pid", "Pid");
            bulk.ColumnMappings.Add("Tid", "Tid");
            bulk.ColumnMappings.Add("Level", "Level");
            bulk.ColumnMappings.Add("Component", "Component");
            bulk.ColumnMappings.Add("Content", "Content");

            return bulk;
        }
    }
}
