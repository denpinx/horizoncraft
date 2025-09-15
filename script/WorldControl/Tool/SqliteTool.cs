using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Godot;
using horizoncraft.script.Net;
using Microsoft.Data.Sqlite;

namespace horizoncraft.script.WorldControl.Tool;

public static class SqliteTool
{
    private static bool IsEnableWAL;
    private static string LastWalWorld = "";

    public static SqliteConnection InitSqlite(string worldName)
    {
        try
        {
            if (worldName == "")
            {
                GD.PrintErr($"{worldName} is empy!");
            }

            if (worldName != LastWalWorld)
            {
                IsEnableWAL = false;
            }

            LastWalWorld = worldName;


            SqliteConnection sqliteConnection;
            if (!DirAccess.DirExistsAbsolute($"save"))
            {
                Error err = DirAccess.MakeDirAbsolute($"save");
                if (err != Error.Ok)
                    GD.PrintErr($"创建 save 文件夹失败，错误码: {err}");
            }

            if (!DirAccess.DirExistsAbsolute($"save/{worldName}"))
            {
                Error err = DirAccess.MakeDirAbsolute($"save/{worldName}");
                if (err != Error.Ok)
                    GD.PrintErr($"创建 save{worldName} 文件夹失败，错误码: {err}");
            }

            sqliteConnection = new SqliteConnection(
                $"Data Source=save/{worldName}/data.db"
            );
            sqliteConnection.Open();

            if (!IsEnableWAL)
            {
                if (sqliteConnection.EnableWAL())
                    IsEnableWAL = true;
            }

            sqliteConnection.InitTable_WorldProfile();
            sqliteConnection.InitTable_World();
            sqliteConnection.InitTable_Player();
            return sqliteConnection;
        }
        catch (SqliteException ex)
        {
            GD.PrintErr(ex.Message);
        }

        return null;
    }

    public static void UpdateChunkByteData(this SqliteConnection sqliteConnection, int x, int y, Chunk chunk)
    {
        var bytes = ByteTool.ToBytes<Chunk>(chunk);

        string query = "UPDATE World SET byte = @Byte WHERE x = @KeyX AND y = @KeyY";
        using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
        {
            command.Parameters.AddWithValue("@Byte", bytes);
            command.Parameters.AddWithValue("@KeyX", x);
            command.Parameters.AddWithValue("@KeyY", y);
            command.ExecuteNonQuery();
        }
    }

    public static void InsertChunkByteValue(this SqliteConnection sqliteConnection, int x, int y, Chunk chunk)
    {
        var bytes = ByteTool.ToBytes<Chunk>(chunk);
        string query = "INSERT INTO World (x, y, byte) VALUES (@KeyX, @KeyY, @Byte)";
        using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
        {
            command.Parameters.AddWithValue("@Byte", bytes);
            command.Parameters.AddWithValue("@KeyX", x);
            command.Parameters.AddWithValue("@KeyY", y);
            command.ExecuteNonQuery();
        }
    }

    public static Chunk GetChunkByteData(this SqliteConnection sqliteConnection, int x, int y)
    {
        string query = "SELECT byte FROM World WHERE x = @KeyX AND y = @KeyY";
        using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
        {
            command.Parameters.AddWithValue("@KeyX", x);
            command.Parameters.AddWithValue("@KeyY", y);
            var result = command.ExecuteScalar() as byte[];
            return ByteTool.FromBytes<Chunk>(result);
        }
    }

    public static bool CheckChunkExists(this SqliteConnection sqliteConnection, int x, int y)
    {
        string query = "SELECT COUNT(*) FROM World WHERE x = @x AND y = @y";
        using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
        {
            command.Parameters.AddWithValue("@x", x);
            command.Parameters.AddWithValue("@y", y);
            int count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }
    }

    public static bool CheckPlayerExists(this SqliteConnection sqliteConnection, string name)
    {
        string query = "SELECT COUNT(*) FROM Player WHERE name = @Name";
        using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
        {
            command.Parameters.AddWithValue("@Name", name);
            int count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }
    }

    public static void UpdatePlayerByteData(this SqliteConnection sqliteConnection, string name, PlayerData playerData)
    {
        var bytes = ByteTool.ToBytes(playerData);
        string query = "UPDATE Player SET byte = @Byte WHERE name = @Name";
        using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
        {
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Byte", bytes);
            command.ExecuteNonQuery();
        }
    }

    public static void InsertPlayerByteValue(this SqliteConnection sqliteConnection, string name, PlayerData playerData)
    {
        var bytes = ByteTool.ToBytes(playerData);

        string query = "INSERT INTO Player (name,byte) VALUES (@Name, @Byte)";
        using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
        {
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Byte", bytes);
            command.ExecuteNonQuery();
        }
    }

    public static PlayerData GetPlayerByteData(this SqliteConnection sqliteConnection, string name)
    {
        string query = "SELECT byte FROM Player WHERE name = @Name";
        using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
        {
            command.Parameters.AddWithValue("@Name", name);
            var result = command.ExecuteScalar() as byte[];
            return ByteTool.FromBytes<PlayerData>(result);
        }
    }

    public static void InsertWorldProfileByteValue(this SqliteConnection sqliteConnection, string name,
        WorldProfile worldProfile)
    {
        var bytes = ByteTool.ToBytes<WorldProfile>(worldProfile);
        string query = "INSERT INTO WorldProfile (name,byte) VALUES (@Name, @Byte)";
        using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
        {
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Byte", bytes);
            command.ExecuteNonQuery();
        }
    }

    public static bool CheckWorldProfileExists(this SqliteConnection sqliteConnection, string name)
    {
        string query = "SELECT COUNT(*) FROM WorldProfile WHERE name = @Name";
        using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
        {
            command.Parameters.AddWithValue("@Name", name);
            int count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }
    }

    public static void UpdateWorldProfileByteData(this SqliteConnection sqliteConnection, string name,
        WorldProfile profile)
    {
        var bytes = ByteTool.ToBytes<WorldProfile>(profile);
        string query = "UPDATE WorldProfile SET byte = @Byte WHERE name = @Name";
        using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
        {
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Byte", bytes);
            command.ExecuteNonQuery();
        }
    }

    public static WorldProfile GetWorldProfileByteData(this SqliteConnection sqliteConnection, string name)
    {
        string query = "SELECT byte FROM WorldProfile WHERE name = @Name";
        using (SqliteCommand command = new SqliteCommand(query, sqliteConnection))
        {
            command.Parameters.AddWithValue("@Name", name);
            var result = command.ExecuteScalar() as byte[];
            return ByteTool.FromBytes<WorldProfile>(result);
        }
    }

    public static void InitTable_World(this SqliteConnection sqliteConnection)
    {
        //GD.Print($"InitTable_World()");
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

    public static void InitTable_Player(this SqliteConnection sqliteConnection)
    {
        //GD.Print($"InitTable_Player()");
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

    public static void InitTable_WorldProfile(this SqliteConnection sqliteConnection)
    {
        //GD.Print($"InitTable_WorldProfile()");
        const string createTableQuery =
            $"CREATE TABLE IF NOT EXISTS WorldProfile ("
            + "id INTEGER PRIMARY KEY AUTOINCREMENT, "
            + "name TEXT NOT NULL UNIQUE, "
            + "byte BLOB "
            + ")";
        new SqliteCommand(createTableQuery, sqliteConnection).ExecuteNonQuery();
    }

    public static bool EnableWAL(this SqliteConnection sqliteConnection)
    {
        using (var cmd = new SqliteCommand("PRAGMA journal_mode = WAL;", sqliteConnection))
        {
            var result = cmd.ExecuteScalar()?.ToString().ToLower();
            return (result == "wal");
        }
    }

    public static void SafeCloseWAL(this SqliteConnection conn)
    {
        if (conn?.State != System.Data.ConnectionState.Open) return;
        IsEnableWAL = false;
        LastWalWorld = "";

        try
        {
            using (var cmd = new SqliteCommand("PRAGMA wal_checkpoint;", conn))
            {
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqliteCommand("PRAGMA journal_mode=DELETE;", conn))
            {
                cmd.ExecuteScalar(); // 必须读取结果
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WAL 清理失败: {ex.Message}");
        }
        finally
        {
            conn.Close();
            conn.Dispose();
        }
    }
}