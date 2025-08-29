using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Godot;
using Godot.Collections;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Config;
using horizoncraft.script.Entity;
using horizoncraft.script.Features;
using horizoncraft.script.Inventory;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Service;
using HorizonCraft.script.WorldControl.Service;
using Vector3 = System.Numerics.Vector3;

namespace horizoncraft.script;

public partial class Player : CharacterBody2D
{
    private const bool TEST_MODE = true;
    public static LocalProfile Profile;
    public static List<Func<string>> GetInformation = new List<Func<string>>();

    public Action OnMoveToChunk;
    public PlayerData playerData;
    public World world;
    public const float Speed = 300.0f;
    public const float JumpVelocity = -200.0f;
    public bool Inputable = true;
    public bool MoreInfo = false;
    [Export] public bool fly = true;

    public bool Stop = false;

    //
    Camera2D camera2d;
    Timer Timer_Tick;
    Label Label_DEBUG_Left;
    Label Label_DEBUG_Right;
    Label Label_PlayerName;
    CollisionShape2D collisionShape2D;

    public InventoryNode ShowView;
    public Sprite2D Cursor;
    public HotBar hotBar;
    public RayCast2D RayCast;
    //

    PlayerBreakProcess BreakProcess = new PlayerBreakProcess();


    private bool LastFramIsLeft = false;
    private bool LastFramIsRight = false;

    private Vector2I LastFramPosition;

    public override void _Input(InputEvent @event)
    {
        if (!Inputable) return;
        // List<Vector2I> interpolatePos = new();
        // if (LastFramIsLeft || LastFramIsRight)
        // {
        //     var v1 = LastFramPosition - Mousecoord;
        //     var v2 = v1.Abs();
        //     var v3 = (Vector2)LastFramPosition;
        //     var v4 = (Vector2)Mousecoord;
        //     var t = (int)MathF.Max(v2.X, v2.Y);
        //     for (int i = 0; i < t; i++)
        //     {
        //         interpolatePos.Add((Vector2I)v3.Lerp(v4, (float)i / (float)t));
        //     }
        // }
    }

    public override void _Process(double delta)
    {
        if (!Inputable) return;

        Vector2I Mousecoord = new(
            (int)Mathf.Floor(GetGlobalMousePosition().X / 16),
            (int)Mathf.Floor(GetGlobalMousePosition().Y / 16)
        );

        if (Input.IsActionPressed("breakblock") && playerData != null && ShowView == null)
        {
            var pos = Mousecoord;
            var pos0 = new Vector3I(pos.X, pos.Y, 0);
            var pos1 = new Vector3I(pos.X, pos.Y, 1);
            var block1 = world.WorldService.GetBlock(pos0);
            var block2 = world.WorldService.GetBlock(pos1);

            if (block1 == null || block2 == null) return;

            Vector3I finalpos;
            Blockdata InterfaceBlock;
            if (!block2.IsMeta("air"))
            {
                finalpos = pos1;
                InterfaceBlock = block2;
            }
            else
            {
                finalpos = pos0;
                InterfaceBlock = block1;
            }

            if (InterfaceBlock.IsMeta("air")) return;

            if (BreakProcess.ProcessTime >= BreakProcess.FinalTime)
            {
                world.WorldService.BreakBlock(playerData, BreakProcess.Position);
                BreakProcess.ProcessTime = 0;
                BreakProcess.FinalTime = 2;
            }
            else if (BreakProcess.ProcessTime > 0)
            {
                BreakProcess.ProcessTime += (float)delta;

                if (BreakProcess.Position != finalpos)
                    BreakProcess.Reset();
            }
            else
            {
                BreakProcess.FinalTime = InterfaceBlock.BlockMeta.Rigidity;
                BreakProcess.Position = finalpos;
                BreakProcess.ProcessTime += (float)delta;

                if (playerData.Mode == 1) BreakProcess.FinalTime = 0;
            }
        }
        else
        {
            if (BreakProcess.ProcessTime > 0) BreakProcess.ProcessTime = 0;
        }

        if (Input.IsActionPressed("placeblock") && playerData != null && ShowView == null)
        {
            var targetpos = new Vector3I(Mousecoord.X, Mousecoord.Y, 0);
            if (!IsInRange(targetpos.X * 16, targetpos.Y * 16) && world.WorldService.PlaceBlock(playerData, targetpos))
            {
            }
            else
            {
                world.WorldService.InterfaceBlock(playerData, targetpos);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (playerData == null) return;
        if (playerData.Name != Player.Profile.Name) return;

        Vector2I Mcoord = World.MathFloor((Vector2I)GetGlobalMousePosition(), 16);

        Cursor.GlobalPosition = Mcoord * 16;
        if (BreakProcess.ProcessTime >= 0)
            Cursor.Frame = (int)(8 * (BreakProcess.ProcessTime / BreakProcess.FinalTime));
        else Cursor.Frame = 0;

        Vector2I MCcoord = World.MathFloor(Mcoord, Chunk.Size);
        Vector2I ChunkCoord = new Vector2I(
            (int)Mathf.Floor(Position.X / (16 * Chunk.Size)),
            (int)Mathf.Floor(Position.Y / (16 * Chunk.Size))
        );
        if (ChunkCoord != playerData.ChunkCoord)
        {
            if (OnMoveToChunk != null)
                OnMoveToChunk.Invoke();
        }

        //更新自身数据
        playerData.Position = new System.Numerics.Vector2(Position.X, Position.Y);
        Label_PlayerName.Text = playerData.Name;

        if (world.WorldService is WorldHostService whs && world.WorldService is WorldBase _worldBase_)
        {
            if (_worldBase_.Players.ContainsKey(playerData.Name))
                _worldBase_.Players[playerData.Name].Position = playerData.Position;
        }

        //防止加载地形的时候卡墙里
        if (world == null || !world.WorldService.Chunks.ContainsKey(ChunkCoord))
        {
            Stop = true;
        }
        else
        {
            if (playerData.Mode == 0)
                Stop = false;
        }

        if (Inputable)
        {
            if (playerData != null)
            {
                if (Input.IsActionJustPressed("roller_up"))
                {
                    playerData.Inventory.HandSlot -= 1;
                    if (playerData.Inventory.HandSlot < 0) playerData.Inventory.HandSlot = 8;
                    if (world.WorldService is WorldClientService wcs)
                        world.RpcId(1, "SetHandSlot", playerData.Name, playerData.Inventory.HandSlot);
                }

                if (Input.IsActionJustPressed("roller_down"))
                {
                    playerData.Inventory.HandSlot += 1;
                    if (playerData.Inventory.HandSlot > 8) playerData.Inventory.HandSlot = 0;
                    if (world.WorldService is WorldClientService wcs)
                        world.RpcId(1, "SetHandSlot", playerData.Name, playerData.Inventory.HandSlot);
                }

                if (Input.IsActionJustPressed("e"))
                {
                    if (ShowView != null)
                    {
                        if (world.WorldService is WorldClientService wcs)
                        {
                            world.RpcId(1, "CloseBlockInv", Player.Profile.Name);
                        }

                        world.player.playerData.OpeningBlockInventory = false;

                        RemoveChild(ShowView);
                        ShowView = null;
                    }
                    else
                    {
                        world.WorldService.OpenView("PlayerInventory");
                    }
                }
            }

            if (playerData.Mode == 0)
            {
                Vector2 velocity = Velocity;
                if (!IsOnFloor() && (!fly || !Stop))
                {
                    velocity += GetGravity() * (float)delta;
                }

                if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
                {
                    velocity.Y = JumpVelocity;
                }

                Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
                if (direction != Vector2.Zero)
                {
                    velocity.X = direction.X * Speed;
                }
                else
                {
                    velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
                }

                Velocity = velocity;
                MoveAndSlide();
            }

            if (playerData.Mode == 1)
            {
                if (Input.IsActionPressed("zoom_1"))
                    camera2d.Zoom = new(2, 2);
                if (Input.IsActionPressed("zoom_2"))
                    camera2d.Zoom = new(1, 1);
                if (Input.IsActionPressed("zoom_3"))
                    camera2d.Zoom = new(0.5f, 0.5f);
                if (Input.IsActionPressed("zoom_4"))
                    camera2d.Zoom = new(0.25f, 0.25f);
                Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

                Position += direction * 64;
            }

            if (TEST_MODE)
            {
                if (Input.IsActionJustPressed("TEST_1"))
                {
                    //生成生物
                    if (EntityManage.Enable)
                    {
                        EntityMeta entityMeta = Materials.GetEntityMeta("item_entity");
                        EntityNode entity = entityMeta.GetEntityNode();
                        entity.Data.position = new(GetGlobalMousePosition().X, GetGlobalMousePosition().Y);
                        EntityManage.waitEntitys.Add(entity);
                    }
                }

                if (Input.IsActionJustPressed("F1") && Inputable)
                    playerData.Mode = playerData.Mode == 0 ? 1 : 0;
                if (Input.IsActionJustPressed("F2") && Inputable)
                    DebugView.DEBUG = !DebugView.DEBUG;
                if (Input.IsActionJustPressed("F3") && Inputable)
                {
                    MoreInfo = !MoreInfo;
                    Label_DEBUG_Left.Visible = MoreInfo;
                    Label_DEBUG_Right.Visible = MoreInfo;
                }

                if (Input.IsActionJustPressed("F4") && Inputable)
                {
                    foreach (var item in Materials.itemmetas)
                    {
                        playerData.Inventory.TryAddItem(item.GetItemStack());
                    }
                }

                if (Input.IsActionJustPressed("F5") && Inputable)
                {
                    Position = new Vector2(new Random().Next(100000), Position.Y);
                    OnMoveToChunk?.Invoke();
                }
            }
        }
    }

    public void UpdateGui()
    {
        if (MoreInfo && Inputable &&
            world.WorldService is WorldBase worldBase && playerData != null)
        {
            Vector2I Mcoord = World.MathFloor((Vector2I)GetGlobalMousePosition(), 16);
            Vector2I MCcoord = World.MathFloor(Mcoord, Chunk.Size);
            Label_DEBUG_Left.Text = "";
            StringBuilder Text = new StringBuilder();

            var targetblock = world?.WorldService?.GetBlock(new Vector3I(Mcoord.X, Mcoord.Y, 1));

            Text.AppendLine($"全局A坐标：{playerData.Coord.X},{playerData.Coord.Y}");
            Text.AppendLine($"区块坐标：{playerData.ChunkCoord.X},{playerData.ChunkCoord.Y}");
            Text.AppendLine($"加载区块：{world.WorldService.Chunks.Count}");
            Text.AppendLine($"正在加载：{world.WorldService.LoadChunkQueue.Count}");
            Text.AppendLine($"TileMap: {world.tileMapLayerChunks.Count}");
            Text.AppendLine($"显示区块: {world.VisibleChunks.Count}");
            Text.AppendLine($"鼠标位置: {Mcoord.X},{Mcoord.Y} ");
            if (targetblock != null)
                Text.AppendLine($"光照: {targetblock.Light} ");
            Text.AppendLine($"当前方块坐标: {MCcoord.X},{MCcoord.Y} ");
            Text.AppendLine($"World更新耗时: {world.tick_use_time}MS");
            Text.AppendLine($"时刻更新耗时: {world.WorldService.TickConsuming}MS");
            Text.AppendLine($"光照更新耗时: {world.WorldService.LightConsuming}MS");
            Text.AppendLine($"加载失败计数: {world.WorldService.UnloadCount.Count}");
            if (world.WorldService is WorldHostService hostserver)
            {
                Text.AppendLine($"区块同步耗时: {hostserver.SyncChunkTime.X}ms max: {hostserver.SyncChunkTime.Y} ms");
                Text.AppendLine($"玩家同步耗时: {hostserver.SyncPlayerTime.X}ms max: {hostserver.SyncPlayerTime.Y} ms");
            }

            if (world.WorldService is WorldClientService wcs)
            {
                Text.AppendLine($"接收的增量更新包: {wcs.ReciveChunkPacks.Count}");
            }

            Text.AppendLine($"时间: {world.WorldService.TickTimes}");
            Text.AppendLine($"在线玩家: {worldBase.Players.Count}");
            Text.AppendLine($"加载玩家实体: {world.PlayerNodes.Count}");
            foreach (Func<string> func in GetInformation)
                Text.AppendLine(func());
            Label_DEBUG_Left.Text = Text.ToString();
            StringBuilder right = new StringBuilder();
            foreach (var sets in worldBase.Players)
            {
                right.AppendLine(
                    $"在线玩家[{sets.Key}] 坐标:[{sets.Value.ChunkCoord.X},{sets.Value.ChunkCoord.Y}],id{sets.Value.PeerId}");
            }

            foreach (var sets in world.WorldService.LoadingPlayers)
            {
                right.AppendLine($"待加载信息:玩家[{sets}]");
            }

            Label_DEBUG_Right.Text = right.ToString();
        }
    }

    public override void _Ready()
    {
        collisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
        Cursor = GetNode<Sprite2D>("Cursor");
        camera2d = GetNode<Camera2D>("Camera2D");
        Label_DEBUG_Left = GetNode<Label>("CanvasLayer/Control/Label_DEBUG_Left");
        Label_DEBUG_Right = GetNode<Label>("CanvasLayer/Control/Label_DEBUG_Right");
        Label_PlayerName = GetNode<Label>("Label_PlayerName");
        Timer_Tick = GetNode<Timer>("Timer_Tick");
        hotBar = GetNode<HotBar>("CanvasLayer/HotBar");
        if (playerData != null)
            playerData.player = this;
        Timer_Tick.Timeout += UpdateGui;
        hotBar.Player = this;
        ;
    }


    public bool IsInRange(int x, int y, float w = 16f, float h = 16f)
    {
        var rect1 = collisionShape2D.Shape.GetRect();
        var rect2 = new Rect2(collisionShape2D.GlobalPosition + rect1.Position, rect1.Size);
        var rec3 = new Rect2(x, y, w, h);
        if (rect2.Intersects(rec3))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}