using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Godot;
using Godot.Collections;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Entity;
using horizoncraft.script.Features;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Service;
using HorizonCraft.script.WorldControl.Service;

public partial class Player : CharacterBody2D
{
    public static string LocalName = "Player";
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

    public bool LastFramIsLeft = false;

    public override void _Input(InputEvent @event)
    {
        if (!Inputable) return;
        Vector2I Mousecoord = new(
            (int)Mathf.Floor(GetGlobalMousePosition().X / 16),
            (int)Mathf.Floor(GetGlobalMousePosition().Y / 16)
        );
        if (Input.IsActionPressed("breakblock"))
        {
            Blockdata blockdata = world.WorldService.GetBlock(new(Mousecoord.X, Mousecoord.Y, 1));
            if (blockdata?.components.Count > 0)
            {
                GD.Print($"点击方块名：{blockdata.BlockMeta.NAME}");
                GD.Print($"点击方块状态：{blockdata.STATE}");
                for (int i = 0; i < blockdata.components.Count; i++)
                {
                    if (blockdata.components[i] != null)
                    {
                        if (blockdata.components[i] is TickComponent tc)
                            GD.Print($"当前 {tc.Current},最大{tc.Max} 组件名{tc.Name}");
                        else
                        {
                            GD.PrintErr(blockdata.components[i].GetType());
                            GD.PrintErr(blockdata.components[i].Name);
                        }

                        if (blockdata.components[i] is FluidComponent fc)
                        {
                            GD.Print($"流体名:{fc.Name},是否流动：{fc.mobility}");
                        }
                    }
                    else
                    {
                        GD.Print("ISnull!");
                    }
                }
            }

            if (LastFramIsLeft)
            {
                // world.chunkManage.SetBlock(
                //     new(Mousecoord.X, Mousecoord.Y, 1),
                //     Materials.Valueof("stone")
                // );
            }
            else
            {
                LastFramIsLeft = true;
            }
        }
        else
        {
            LastFramIsLeft = false;
        }

        if (Input.IsActionPressed("placeblock"))
        {
            world.WorldService.SetBlock(
                new(Mousecoord.X, Mousecoord.Y, 1),
                Materials.Valueof("air")
            );
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (playerData == null) return;
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


        if (MoreInfo && Inputable && world.WorldService is WorldBase worldBase)
        {
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


            if (world.WorldService is WorldHostService hostserver)
            {
                Text.AppendLine($"区块同步耗时: {hostserver.SyncChunkTime.X} max: {hostserver.SyncChunkTime.Y}");
                Text.AppendLine($"玩家同步耗时: {hostserver.SyncPlayerTime.X} max: {hostserver.SyncPlayerTime.Y}");
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

        if (mode == 0 && Inputable)
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

        if (mode == 1 && Inputable)
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
    }

    public override void _Ready()
    {
        camera2d = GetNode<Camera2D>("Camera2D");
        Label_DEBUG_Left = GetNode<Label>("CanvasLayer/Control/Label_DEBUG_Left");
        Label_DEBUG_Right = GetNode<Label>("CanvasLayer/Control/Label_DEBUG_Right");
        Label_PlayerName = GetNode<Label>("Label_PlayerName");
        Timer_Tick = GetNode<Timer>("Timer_Tick");
        if (playerData != null)
            playerData.player = this;
    }
}