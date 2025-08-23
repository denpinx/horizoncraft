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

public partial class Player : CharacterBody2D
{
    private const bool TEST_MODE = true;
    public static PlayerProfile Profile;
    public static List<Func<string>> GetInformation = new List<Func<string>>();

    //
    public Action OnMoveToChunk;
    public PlayerData playerData;
    public World world;
    public const float Speed = 300.0f;
    public const float JumpVelocity = -200.0f;
    public bool Inputable = true;
    public bool MoreInfo = false;
    [Export] public int mode = 0;
    [Export] public bool fly = true;
    public bool Stop = false;
    Camera2D camera2d;
    Timer Timer_Tick;
    Label Label_DEBUG_Left;
    Label Label_DEBUG_Right;
    Label Label_PlayerName;

    public InventoryNode ShowView;

    public HotBar hotBar;

    private bool LastFramIsLeft = false;
    private bool LastFramIsRight = false;

    private Vector2I LastFramPosition;

    public override void _Input(InputEvent @event)
    {
        if (!Inputable) return;
        Vector2I Mousecoord = new(
            (int)Mathf.Floor(GetGlobalMousePosition().X / 16),
            (int)Mathf.Floor(GetGlobalMousePosition().Y / 16)
        );
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

        if (Input.IsActionPressed("breakblock") && playerData != null && ShowView == null)
        {
            var targetpos = new Vector3I(Mousecoord.X, Mousecoord.Y, 1);
            var fontblock = world.WorldService.GetBlock(targetpos);
            if (fontblock == null) return;
            if (fontblock.IsMeta("air")) targetpos = new Vector3I(Mousecoord.X, Mousecoord.Y, 0);
            var fblock = world.WorldService.GetBlock(targetpos);
            if (!world.WorldService.CheckIsCloseBlock(targetpos))
                if (fblock != null && !fblock.IsMeta("air"))
                {
                    world.WorldService.SetBlock(
                        new(targetpos.X, targetpos.Y, targetpos.Z),
                        Materials.Valueof("air"), false, 0
                    );
                }
        }


        if (Input.IsActionPressed("placeblock") && playerData != null && ShowView == null)
        {
            var targetpos = new Vector3I(Mousecoord.X, Mousecoord.Y, 0);
            var backblock = world.WorldService.GetBlock(targetpos);
            var fontblock = world.WorldService.GetBlock(new Vector3I(Mousecoord.X, Mousecoord.Y, 1));

            if (fontblock != null && backblock != null)
            {
                if (fontblock.IsMeta("air"))
                    if (backblock.IsMeta("air"))
                        targetpos = new Vector3I(Mousecoord.X, Mousecoord.Y, 0);
                    else targetpos = new Vector3I(Mousecoord.X, Mousecoord.Y, 1);
                else if (!fontblock.IsMeta("air"))
                    targetpos = new Vector3I(Mousecoord.X, Mousecoord.Y, 1);
            }
            else return;

            var targetblock = world.WorldService.GetBlock(targetpos);
            if (targetblock != null && targetblock.IsMeta("air"))
            {
                GD.Print("place block");
                //放置方块
                var item = playerData.Inventory.GetItem(playerData.Inventory.HandSlot);
                if (item != null)
                {
                    BlockMeta bm = item.GetBlockMeta();
                    if (bm != null)
                        world.WorldService.SetBlock(
                            targetpos,
                            bm, false, 0
                        );
                    if (mode == 0) playerData.Inventory.SubItemAmount(playerData.Inventory.HandSlot);
                }
                //交互方块
            }
            else
            {
                if (world.WorldService is WorldClientService wcs)
                {
                    playerData.OpeningBlockInventory = true;
                    playerData.OpenInventory = new Vector3(targetpos.X, targetpos.Y, targetpos.Z);
                    GD.Print("Cilent_OpenBlockInv");
                    world.RpcId(1, "OpenBlockInv",
                        Profile.Name,
                        targetpos.X,
                        targetpos.Y,
                        targetpos.Z);
                }
                else
                {
                    var blockinv = targetblock.GetComponent<InventoryComponent>();
                    if (blockinv != null)
                    {
                        GD.Print("打开方块物品栏");
                        world.WorldService.OpenBlockView(blockinv.InventoryName, targetpos.X, targetpos.Y, targetpos.Z);
                    }
                    else
                        GD.Print(
                            $"{targetblock.ID} 组件 InventoryComponent 不存在!,cmp count:{targetblock.components.Count}]");
                }
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (playerData == null) return;
        if (playerData.Name != Player.Profile.Name) return;

        Vector2I Mcoord = World.MathFloor((Vector2I)GetGlobalMousePosition(), 16);
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
            if (mode == 0)
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
                }

                if (Input.IsActionJustPressed("roller_down"))
                {
                    playerData.Inventory.HandSlot += 1;
                    if (playerData.Inventory.HandSlot > 8) playerData.Inventory.HandSlot = 0;
                }

                if (Input.IsActionJustPressed("e"))
                {
                    if (ShowView != null)
                    {
                        if (world.WorldService is WorldClientService wcs)
                        {
                            world.RpcId(1, "CloseBlockInv");
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

            if (mode == 0)
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

            if (mode == 1)
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
                    mode = mode == 0 ? 1 : 0;
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
            Text.AppendLine($"全局A坐标：{playerData.Coord.X},{playerData.Coord.Y}");
            Text.AppendLine($"区块坐标：{playerData.ChunkCoord.X},{playerData.ChunkCoord.Y}");
            Text.AppendLine($"加载区块：{world.WorldService.Chunks.Count}");
            Text.AppendLine($"正在加载：{world.WorldService.LoadChunkQueue.Count}");
            Text.AppendLine($"TileMap: {world.tileMapLayerChunks.Count}");
            Text.AppendLine($"显示区块: {world.VisibleChunks.Count}");
            Text.AppendLine($"鼠标位置: {Mcoord.X},{Mcoord.Y} ");
            Text.AppendLine($"当前方块坐标: {MCcoord.X},{MCcoord.Y} ");
            Text.AppendLine($"World.Tick耗时: {world.tick_use_time}MS");
            Text.AppendLine($"ChunkManage.Tick耗时: {world.WorldService.TickConsuming}MS");
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
}