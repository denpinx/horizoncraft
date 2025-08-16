using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Microsoft.Data.Sqlite;

namespace horizoncraft.script.WorldControl
{
    public partial class WorldManage
    {
        private void InitSqlite()
        {
            try
            {
                if (!DirAccess.DirExistsAbsolute($"save"))
                {
                    Error err = DirAccess.MakeDirAbsolute($"save");
                    if (err != Error.Ok)
                        GD.PrintErr($"创建 save 文件夹失败，错误码: {err}");
                }

                if (!DirAccess.DirExistsAbsolute($"save/{World.world_name}"))
                {
                    Error err = DirAccess.MakeDirAbsolute($"save/{World.world_name}");
                    if (err != Error.Ok)
                        GD.PrintErr($"创建 save{World.world_name} 文件夹失败，错误码: {err}");
                }

                sqliteConnection = new SqliteConnection(
                    $"Data Source=save/{World.world_name}/data.db"
                );
                sqliteConnection.Open();
                InitTable_World();
                InitTable_Player();
            }
            catch (SqliteException ex)
            {
                GD.PrintErr(ex.Message);
            }
        }

        private void UpdateChunkByteData(int x, int y, byte[] bytes)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }

            string query = "UPDATE World SET byte = @Byte WHERE x = @KeyX AND y = @KeyY";
            using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
            {
                command.Parameters.AddWithValue("@Byte", output.ToArray());
                command.Parameters.AddWithValue("@KeyX", x);
                command.Parameters.AddWithValue("@KeyY", y);
                command.ExecuteNonQuery();
            }
        }

        private void InsertChunkByteValue(int x, int y, byte[] bytes)
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

        private byte[] GetChunkByteData(int x, int y)
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

        private bool CheckChunkExists(int x, int y)
        {
            if (worldMode == WorldMode.Preview || worldMode == WorldMode.MultiplayerClient) return false;

            string query = "SELECT COUNT(*) FROM World WHERE x = @x AND y = @y";
            using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
            {
                command.Parameters.AddWithValue("@x", x);
                command.Parameters.AddWithValue("@y", y);
                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
        }

        private bool CheckPlayerExists(string name)
        {
            if (worldMode == WorldMode.Preview || worldMode == WorldMode.MultiplayerClient) return false;

            string query = "SELECT COUNT(*) FROM Player WHERE name = @Name";
            using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
            {
                command.Parameters.AddWithValue("@Name", name);
                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
        }

        private void UpdatePlayerByteData(string name, byte[] bytes)
        {
            //GD.Print($"[{time}] UpdatePlayerByteData(name:{name})");
            // using var output = new MemoryStream();
            // using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
            // {
            //     gzip.Write(bytes, 0, bytes.Length);
            // }

            string query = "UPDATE Player SET byte = @Byte WHERE name = @Name";
            using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
            {
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Byte", bytes);
                command.ExecuteNonQuery();
            }
        }

        private void InsertPlayerByteValue(string name, byte[] bytes)
        {
            // using var output = new MemoryStream();
            // using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
            // {
            //     gzip.Write(bytes, 0, bytes.Length);
            // }

            string query = "INSERT INTO Player (name,byte) VALUES (@Name, @Byte)";
            using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
            {
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Byte", bytes);
                command.ExecuteNonQuery();
            }
        }

        private byte[] GetPlayerByteData(string name)
        {
            string query = "SELECT byte FROM Player WHERE name = @Name";
            using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
            {
                command.Parameters.AddWithValue("@Name", name);
                var result = command.ExecuteScalar() as byte[];
                using var input = new MemoryStream(result);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }

        private void InitTable_World()
        {
            GD.Print($"[{time}] InitTable_World()");
            string createTableQuery =
                $"CREATE TABLE IF NOT EXISTS World ("
                + "id INTEGER PRIMARY KEY AUTOINCREMENT, "
                + "x INTEGER NOT NULL, "
                + "y INTEGER NOT NULL, "
                + "json TEXT, "
                + "byte BLOB, "
                + "UNIQUE(x, y)"
                + ")";
            new SqliteCommand(createTableQuery, sqliteConnection).ExecuteNonQuery();
        }

        private void InitTable_Player()
        {
            GD.Print($"[{time}] InitTable_Player()");
            const string createTableQuery =
                $"CREATE TABLE IF NOT EXISTS Player ("
                + "id INTEGER PRIMARY KEY AUTOINCREMENT, "
                + "name TEXT NOT NULL UNIQUE, "
                + "byte BLOB "
                + ")";
            new SqliteCommand(createTableQuery, sqliteConnection).ExecuteNonQuery();
            const string createidx = @"CREATE UNIQUE INDEX IF NOT EXISTS Player_name ON Player(name)";
            new SqliteCommand(createidx, sqliteConnection).ExecuteNonQuery();
        }
    }
}