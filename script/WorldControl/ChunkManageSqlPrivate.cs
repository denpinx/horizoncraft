using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace horizoncraft.script.WorldControl
{
    public partial class ChunkManageSql
    {
        private void UpdateJsonData(int x, int y, string jsonData)
        {
            string query = "UPDATE World SET json = @JsonData WHERE x = @KeyX AND y = @KeyY";
            using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
            {
                command.Parameters.AddWithValue("@JsonData", jsonData);
                command.Parameters.AddWithValue("@KeyX", x);
                command.Parameters.AddWithValue("@KeyY", y);
                command.ExecuteNonQuery();
            }
        }

        private void InsertNewKeyValue(int x, int y, string jsonData)
        {
            string query = "INSERT INTO World (x, y, json) VALUES (@KeyX, @KeyY, @JsonData)";
            using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
            {
                command.Parameters.AddWithValue("@JsonData", jsonData);
                command.Parameters.AddWithValue("@KeyX", x);
                command.Parameters.AddWithValue("@KeyY", y);
                command.ExecuteNonQuery();
            }
        }

        private string GetJsonData(int x, int y)
        {
            string query = "SELECT json FROM World WHERE x = @KeyX AND y = @KeyY";
            using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
            {
                command.Parameters.AddWithValue("@KeyX", x);
                command.Parameters.AddWithValue("@KeyY", y);
                var result = command.ExecuteScalar() as string;
                return result;
            }
        }
        private void UpdateByteData(int x, int y, byte[] bytes)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }
            ;

            string query = "UPDATE World SET byte = @Byte WHERE x = @KeyX AND y = @KeyY";
            using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
            {
                command.Parameters.AddWithValue("@Byte", output.ToArray());
                command.Parameters.AddWithValue("@KeyX", x);
                command.Parameters.AddWithValue("@KeyY", y);
                command.ExecuteNonQuery();
            }
        }

        private void InsertNewByteValue(int x, int y, byte[] bytes)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }
            ;
            string query = "INSERT INTO World (x, y, byte) VALUES (@KeyX, @KeyY, @Byte)";
            using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
            {
                command.Parameters.AddWithValue("@Byte", output.ToArray());
                command.Parameters.AddWithValue("@KeyX", x);
                command.Parameters.AddWithValue("@KeyY", y);
                command.ExecuteNonQuery();
            }
        }

        private byte[] GetByeData(int x, int y)
        {
            string query = "SELECT byte FROM World WHERE x = @KeyX AND y = @KeyY";
            using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
            {
                command.Parameters.AddWithValue("@KeyX", x);
                command.Parameters.AddWithValue("@KeyY", y);
                var result = command.ExecuteScalar() as byte[];
                using var input = new MemoryStream(result);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}