using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.Features;
using horizoncraft.script.WorldControl.work;
using MemoryPack;
using Microsoft.Data.Sqlite;

namespace horizoncraft.script.WorldControl
{
    public partial class ChunkManageSql : BaseManage
    {
        public event Action<ChunkManageSql, Chunk> OnChunkLoaded;
        public event Action<ChunkManageSql, Chunk> OnChunkUnLoading;
        public long time;
        public long tick_use_time;
        public int LoadHorizon = 3;
        public int TileMapHorizon = 2;
        //已加载区块
        public ConcurrentDictionary<Vector2I, Chunk> LoadedChunks = new();
        //待卸载区块
        public ConcurrentDictionary<Vector2I, Chunk> UnloadingQuee = new();
        // 待加载区块以及加载后处理工作
        public ConcurrentDictionary<Vector2I, WorkBase> LoadingQuee = new();
        public bool Lock = false;
        public World world;
        SqliteConnection sqliteConnection;
        FastNoiseLite fastNoiseLite = new FastNoiseLite();
        public ChunkManageSql(World world) : base("ChunkManageSql")
        {
            this.world = world;
            Horizoncraft.AddManage(this);
            world.timer.Timeout += PreTick;
            world.player.OnMoveToChunk += OnPlayerMoveChunk;
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
                CheckAndCreateTable();
            }
            catch (SqliteException ex)
            {
                GD.PrintErr(ex.Message);
            }
        }

        public void OnPlayerMoveChunk()
        {
            LoadingQuee.Clear();
            Vector2I CenterCoord = world.player.playerData.ChunkCoord;
            for (int X = CenterCoord.X - LoadHorizon; X <= CenterCoord.X + LoadHorizon; X++)
            {
                for (int Y = CenterCoord.Y - LoadHorizon; Y <= CenterCoord.Y + LoadHorizon; Y++)
                {
                    Vector2I coord = new Vector2I(X, Y);
                    if (!LoadedChunks.ContainsKey(coord) && !LoadingQuee.ContainsKey(coord))
                    {
                        LoadingQuee.TryAdd(coord, new WorkBase());
                        GD.Print("加载区块：" + coord);
                    }
                }
            }
            foreach (Vector2I coord in LoadedChunks.Keys)
            {
                Chunk chunk = LoadedChunks[coord];
                //Vector2I Horizon = chunk.coord - world.player.playerData.ChunkCoord;
                if (
                    chunk.coord.X < CenterCoord.X - LoadHorizon
                    || chunk.coord.X > CenterCoord.X + LoadHorizon
                    || chunk.coord.Y < CenterCoord.Y - LoadHorizon
                    || chunk.coord.Y > CenterCoord.Y + LoadHorizon
                )
                {
                    UnloadingQuee[coord] = chunk;
                    LoadedChunks.TryRemove(coord, out _);
                }
            }
        }

        public void PreTick()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            time++;
            Vector2I CenterCoord = world.player.playerData.ChunkCoord;
            for (int X = CenterCoord.X - TileMapHorizon; X <= CenterCoord.X + TileMapHorizon; X++)
            {
                for (
                    int Y = CenterCoord.Y - TileMapHorizon;
                    Y <= CenterCoord.Y + TileMapHorizon;
                    Y++
                )
                {
                    Vector2I coord = new Vector2I(X, Y);
                    if (LoadedChunks.ContainsKey(coord))
                    {
                        world.VisibleChunks[coord] = LoadedChunks[coord];
                    }
                }
            }

            // 记录需要移除的键
            var keysToRemove = new List<Vector2I>();
            foreach (Vector2I coord in world.VisibleChunks.Keys)
            {
                Chunk chunk = world.VisibleChunks[coord];
                Vector2I Horizon = chunk.coord - world.player.playerData.ChunkCoord;
                Horizon.X = Mathf.Abs(Horizon.X);
                Horizon.Y = Mathf.Abs(Horizon.Y);

                // 合并判断条件
                if (
                    Horizon.X > TileMapHorizon
                    || Horizon.Y > TileMapHorizon
                    || !LoadedChunks.ContainsKey(coord)
                )
                {
                    keysToRemove.Add(coord);
                }
            }

            // 移除不可见的区块
            foreach (var key in keysToRemove)
            {
                world.VisibleChunks.Remove(key, out _);
            }

            foreach (Vector2I coord in UnloadingQuee.Keys)
            {
                Lock = true;
                int max = UnloadingQuee.Count;
                int count = 0;
                //Vector2I coord = UnloadingQuee.Keys.Last();
                Chunk chunk = UnloadingQuee[coord];
                Chunk outchunk;
                if (UnloadingQuee.TryRemove(coord, out outchunk))
                {
                    OnChunkUnLoading?.Invoke(this, chunk);
                    Task.Run(() =>
                    {
                        GD.Print("保存", count++, "/", max);
                        if (CheckKeyValueExists(coord.X, coord.Y))
                        {
                            UpdateData(chunk);
                        }
                        else
                        {
                            InsertData(chunk);
                        }
                    });
                    Lock = false;
                }
            }
            foreach (Vector2I coord in LoadingQuee.Keys)
            {
                int max = LoadingQuee.Count;
                //Vector2I coord = LoadingQuee.Keys;
                WorkBase work = LoadingQuee[coord];
                LoadingQuee.TryRemove(coord, out _);
                Task.Run(() =>
                {
                    Chunk chunk;
                    if (CheckKeyValueExists(coord.X, coord.Y))
                    {
                        chunk = GetChunkData(coord.X, coord.Y);
                        LoadedChunks[coord] = chunk;
                        if (work.Type != "NONE")
                            work.Execute(chunk);
                        if (!chunk.spawn)
                            SpawnChunk(chunk);
                    }
                    else
                    {
                        chunk = new(coord.X, coord.Y);
                        LoadedChunks[coord] = chunk;
                        if (work.Type != "NONE")
                            work.Execute(chunk);
                        SpawnChunk(chunk);
                    }
                    OnChunkLoaded?.Invoke(this, chunk);
                });
            }

            foreach (Vector2I coord in LoadedChunks.Keys)
            {
                Chunk chunk = LoadedChunks[coord];
                chunk.Tick(this);
            }

            stopwatch.Stop();
            tick_use_time = stopwatch.ElapsedMilliseconds;
        }

        bool CheckKeyValueExists(int x, int y)
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

        private void CheckAndCreateTable()
        {
            string tableName = "World";
            string checkTableQuery =
                $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
            using (
                SqliteCommand checkTableCommand = new SqliteCommand(
                    checkTableQuery,
                    sqliteConnection
                )
            )
            {
                var result = checkTableCommand.ExecuteScalar();
                if (result == null)
                {
                    string createTableQuery =
                        $"CREATE TABLE {tableName} ("
                        + "id INTEGER PRIMARY KEY AUTOINCREMENT, "
                        + "x INTEGER NOT NULL, "
                        + "y INTEGER NOT NULL, "
                        + "json TEXT, "
                        + "byte BLOB, "
                        + "UNIQUE(x, y)"
                        + ")";
                    using (
                        SqliteCommand createTableCommand = new SqliteCommand(
                            createTableQuery,
                            sqliteConnection
                        )
                    )
                    {
                        createTableCommand.ExecuteNonQuery();
                    }
                    GD.Print("create table");
                }
                else
                {
                    GD.Print("Has Table");
                }
            }
        }
        public void UpdateData(Chunk chunk)
        {
            UpdateByteData(chunk.coord.X, chunk.coord.Y, MemoryPackSerializer.Serialize<Chunk>(chunk));
        }
        public void InsertData(Chunk chunk)
        {
            InsertNewByteValue(chunk.coord.X, chunk.coord.Y, MemoryPackSerializer.Serialize<Chunk>(chunk));
        }
        public Chunk GetChunkData(int x, int y)
        {
            byte[] bytes = GetByeData(x, y);
            Chunk chunk = MemoryPackSerializer.Deserialize<Chunk>(bytes);
            return chunk;
        }

        /// <summary>
        /// 根据区块坐标和本地坐标设置区块中的方块元数据。
        /// </summary>
        /// <param name="chunk">区块</param>
        /// <param name="coord">本地坐标</param>
        /// <param name="meta">方块类型</param>
        /// <param name="replaceAir">是否替换空气</param>
        public void SetBlock(Chunk chunk, Vector3I coord, BlockMeta meta, bool replaceAir = false,int state=0)
        {
            Vector2I LocalCoord = new Vector2I(coord.X, coord.Y);
            Vector2I globalCoord = chunk.coord * Chunk.Size + LocalCoord;
            SetBlock(new(globalCoord.X, globalCoord.Y, coord.Z), meta, replaceAir,state);

            //GD.Print("SetBlock", globalCoord, ",", chunk.coord);
        }

        /// <summary>
        /// 设置方块
        /// </summary>
        /// <param name="coord">全局坐标</param>
        /// <param name="meta">方块类型</param>
        /// <param name="replaceAir">是否替换空气</param>
        public void SetBlock(Vector3I coord, BlockMeta meta, bool replaceAir = false, int state = 0)
        {
            Vector2I ChunkCoord = World.MathFloor(coord, Chunk.Size);
            Vector2I LocalCoord = World.Remainder(coord, Chunk.Size);
            if (LoadedChunks.ContainsKey(ChunkCoord))
            {
                Chunk chunk = LoadedChunks[ChunkCoord];
                if (replaceAir)
                {
                    if (chunk[LocalCoord.X, LocalCoord.Y, coord.Z].IsMeta("air"))
                    {
                        chunk[LocalCoord.X, LocalCoord.Y, coord.Z] = meta.Blockdata();
                        chunk[LocalCoord.X, LocalCoord.Y, coord.Z].STATE = state;
                    }

                }
                else
                {
                    chunk[LocalCoord.X, LocalCoord.Y, coord.Z] = meta.Blockdata();
                    chunk[LocalCoord.X, LocalCoord.Y, coord.Z].STATE = state;
                }
            }
            else
            {
                if (LoadingQuee.ContainsKey(ChunkCoord))
                {
                    if (LoadingQuee[ChunkCoord].Type == "NONE")
                    {
                        SetBlockWork sbw = new SetBlockWork()
                        {
                            Type = "SETBLOCK",
                            ExclList = new List<(Vector3I, BlockMeta, int)>(),
                        };
                        sbw.ExclList.Add((new Vector3I(LocalCoord.X, LocalCoord.Y, coord.Z), meta, state));
                        LoadingQuee[ChunkCoord] = sbw;
                    }
                    else
                    {
                        SetBlockWork stw = (SetBlockWork)LoadingQuee[ChunkCoord];
                        stw.ExclList.Add((new Vector3I(LocalCoord.X, LocalCoord.Y, coord.Z), meta, state));
                    }
                }
                else
                {
                    SetBlockWork sbw = new SetBlockWork()
                    {
                        Type = "SETBLOCK",
                        ExclList = new List<(Vector3I, BlockMeta, int)>(),
                    };
                    sbw.ExclList.Add((new Vector3I(LocalCoord.X, LocalCoord.Y, coord.Z), meta, state));
                    LoadingQuee[ChunkCoord] = sbw;
                }
            }
        }

        /// <summary>
        /// 获取方块,如果因为区块未加载则返回null
        /// </summary>
        /// <param name="coord">全局坐标</param>
        /// <returns>方块数据</returns>
        public Blockdata GetBlock(Vector3I coord)
        {
            Vector2I ChunkCoord = World.MathFloor(coord, Chunk.Size);
            Vector2I LocalCoord = World.Remainder(coord, Chunk.Size);
            if (LoadedChunks.ContainsKey(ChunkCoord))
            {
                Chunk chunk = LoadedChunks[ChunkCoord];
                return chunk[LocalCoord.X, LocalCoord.Y, coord.Z];
            }
            else
            {
                return null;
            }
        }
        public void SpawnChunk(Chunk chunk)
        {
            WorldGenerator.Generator(chunk);
            return;
            //
            chunk.spawn = true;
            chunk.spawncount++;
            Random random = new Random(chunk.coord.X * Chunk.Size);
            for (int Z = 0; Z < Chunk.SizeZ; Z++)
            {
                for (int X = 0; X < Chunk.Size; X++)
                {
                    int globalX = chunk.coord.X * Chunk.Size + X;
                    int NoiseY =
                        64
                        + (int)(fastNoiseLite.GetNoise2D(chunk.coord.X * Chunk.Size + X, Z) * 16);
                    int localY = World.Remainder(NoiseY, Chunk.Size);
                    int ChunkY = World.MathFloor(NoiseY, Chunk.Size);
                    for (int Y = 0; Y < Chunk.Size; Y++)
                    {
                        int globalY = (chunk.coord.Y * Chunk.Size + Y);
                        int ry = NoiseY - globalY;
                        if (ry == 0)
                        {
                            if (random.Next(2) == 1)
                            {
                                SetBlock(new(globalX, globalY, Z), Materials.Valueof("bush"));
                            }
                            else if (random.Next(4) == 1)
                            {
                                int h = 5 + random.Next(8);
                                for (int log_h = 0; log_h < h; log_h++)
                                {
                                    SetBlock(
                                        new(globalX, globalY - log_h, Z),
                                        Materials.Valueof("oak_log")
                                    );
                                    if (log_h > 4)
                                    {
                                        int angle = random.Next(4);
                                        if (angle == 0)
                                        {
                                            SetBlock(
                                                new(globalX - 1, globalY - log_h, Z),
                                                Materials.Valueof("oak_log"),
                                                true,
                                                1
                                            );
                                            SetBlock(

                                                new(globalX - 2, globalY - log_h, Z),
                                                Materials.Valueof("oak_leaves"),
                                                true,
                                                1
                                            );
                                            SetBlock(
                                                new(globalX - 1, globalY - log_h - 1, Z),
                                                Materials.Valueof("oak_leaves"),
                                                true,
                                                1
                                            );
                                        }
                                        angle = random.Next(4);
                                        if (angle == 1)
                                        {
                                            SetBlock(

                                                new(globalX + 1, globalY - log_h, Z),
                                                Materials.Valueof("oak_log"),
                                                true,
                                                1
                                            );
                                            SetBlock(

                                                new(globalX + 2, globalY - log_h, Z),
                                                Materials.Valueof("oak_leaves"),
                                                true,
                                                1
                                            );
                                            SetBlock(

                                                new(globalX + 1, globalY - log_h - 1, Z),
                                                Materials.Valueof("oak_leaves"),
                                                true,
                                                1
                                            );
                                        }
                                    }
                                }
                            }
                        }
                        // if (ry == -1) SetBlock(chunk, new(X, Y, Z), Materials.Valueof("grass"));
                        // if (ry == -2) SetBlock(chunk, new(X, Y, Z), Materials.Valueof("dirt"));
                        // if (ry == -3) SetBlock(chunk, new(X, Y, Z), Materials.Valueof("dirt"));
                        // if (ry == -4) SetBlock(chunk, new(X, Y, Z), Materials.Valueof("dirt"));
                        // if (ry == -5) SetBlock(chunk, new(X, Y, Z), Materials.Valueof("dirt"));
                        // if (ry < -5) SetBlock(chunk, new(X, Y, Z), Materials.Valueof("stone"));
                        if (ry == -1) SetBlock(new(globalX, globalY, Z), Materials.Valueof("grass"));
                        if (ry == -2) SetBlock(new(globalX, globalY, Z), Materials.Valueof("dirt"));
                        if (ry == -3) SetBlock(new(globalX, globalY, Z), Materials.Valueof("dirt"));
                        if (ry == -4) SetBlock(new(globalX, globalY, Z), Materials.Valueof("dirt"));
                        if (ry == -5) SetBlock(new(globalX, globalY, Z), Materials.Valueof("dirt"));
                        if (ry < -5) SetBlock(new(globalX, globalY, Z), Materials.Valueof("stone"));

                    }
                }
            }
            chunk.update = true;
        }
        public void Save()
        {
            foreach (Vector2I coord in LoadedChunks.Keys)
            {
                Chunk chunk = LoadedChunks[coord];
                OnChunkUnLoading?.Invoke(this, chunk);
                if (CheckKeyValueExists(coord.X, coord.Y))
                {
                    UpdateData(chunk);
                }
                else
                {
                    InsertData(chunk);
                }
            }
        }
    }
}
