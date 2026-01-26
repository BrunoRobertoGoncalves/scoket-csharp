using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static server.Protocol;

namespace server
{
    internal static class LogIngestor
    {
        const string ConnStr =
            @"Server=(localdb)\MSSQLLocalDB;Database=TRANSFER_DB;Integrated Security=true;TrustServerCertificate=True;";

        const int BatchSize = 10_000;

        public static async Task HandleClientAsync(TcpClient client)
        {
            var totalWatch = Stopwatch.StartNew();
            Console.WriteLine($"Client connected.");

            using var net = client.GetStream();
            using var con = new SqlConnection(ConnStr);
            con.Open();

            var dt = CreateTable();
            using var bulk = CreateBulkCopy(con);

            long totalLines = 0;
            int totalFiles = 0;

            try
            {
                while (true)
                {
                    long fileSize = await ReadInt64BEAsync(net);
                    if (fileSize == -1) break;
                    if (fileSize < 0) throw new InvalidOperationException($"fileSize inválido: {fileSize}");

                    ushort nameLen = await ReadUInt16BEAsync(net);
                    byte[] nameBytes = await ReadExactAsync(net, nameLen);
                    string fileName = Encoding.UTF8.GetString(nameBytes);

                    totalFiles++;
                    Console.WriteLine($"Receiving file: {fileName} ({fileSize} bytes)");

                    var fileWatch = Stopwatch.StartNew();

                    using var limited = new LimitedReadStream(net, fileSize);
                    using var reader = new StreamReader(limited, Encoding.UTF8, true, 64 * 1024, true);

                    while (true)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null) break;

                        if (!TryAddRow(line, dt)) continue;

                        totalLines++;

                        if (dt.Rows.Count >= BatchSize)
                            Flush(dt, bulk);
                    }

                    fileWatch.Stop();
                    Console.WriteLine($"File done: {fileName} | Time: {fileWatch.Elapsed}");
                }

                if (dt.Rows.Count > 0)
                    Flush(dt, bulk);

                totalWatch.Stop();
                Console.WriteLine("=====================================");
                Console.WriteLine($"Total files: {totalFiles}");
                Console.WriteLine($"Total lines: {totalLines}");
                Console.WriteLine($"Total time : {totalWatch.Elapsed}");
                Console.WriteLine("=====================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex}");
            }
            finally
            {
                client.Close();
            }
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
            var bulk = new SqlBulkCopy(con, SqlBulkCopyOptions.TableLock, null)
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
