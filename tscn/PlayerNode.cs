using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Godot;
using Godot.Collections;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Components.EntityComponents;
using horizoncraft.script.Components.Item;
using horizoncraft.script.Config;
using horizoncraft.script.Entity;
using horizoncraft.script.Events;
using horizoncraft.script.Events.player;
using horizoncraft.script.Expand;
using horizoncraft.script.Inventory;
using horizoncraft.script.rpc;
using HorizonCraft.script.Services.world;
using horizoncraft.script.WorldControl;
using Vector3 = System.Numerics.Vector3;

namespace horizoncraft.script;

public partial class PlayerNode : CharacterBody2D
{
    private PackedScene PackedSceneDeadView = GD.Load<PackedScene>("res://tscn/Gui/DeadView.tscn");
    private const bool TEST_MODE = true;

    /// <summary>
    /// 获取调试信息委托集合
    /// </summary>
    public static System.Collections.Generic.Dictionary<string, Func<string>> GetInformation = new();

    /// <summary>
    /// 本地玩家文档
    /// </summary>
    public static LocalProfile Profile;

    /// <summary>
    /// 方块挖掘进度
    /// </summary>
    public PlayerActionProcess ActionProcess = new();

    /// <summary>
    /// 跳跃高度
    /// </summary>
    public const float JumpVelocity = -200.0f;

    public Action OnMoveToChunk;
    public PlayerData playerData;
    public bool BaseInputable = true;
    public bool Inputable = true;
    public bool MoreInfo = false;
    public bool Stop = false;
    [Export] public World world;
    [Export] public ChatView ChatView;

    /// <summary>
    /// 当前打开查看的物品栏节点
    /// </summary>
    public InventoryNode OpeningInventoryNode;

    //
    [Export] AnimationPlayer animationPlayer_other;
    [Export] AnimationPlayer animationPlayer_move;
    [Export] private HorizonCraft.tscn.Gui.BlockInfoView BlockInfoView;
    [Export] CollisionShape2D collisionShape2D;
    [Export] public CanvasLayer OvrCanvasLayer;
    [Export] public Timer Timer_Tick;
    [Export] Label Label_DEBUG_Right;
    [Export] Label Label_DEBUG_Left;
    [Export] Label Label_PlayerName;
    [Export] Sprite2D sprite2D_body;
    [Export] public Sprite2D Cursor;
    [Export] public Sprite2D Use_Process;
    [Export] public HotBar hotBar;
    [Export] Camera2D camera2d;
    //


    private bool LastFramIsLeft = false;
    private bool LastFramIsRight = false;

    private int LastFramLayer = 0;
    //private Vector2I LastFramPosition;

    public override void _Process(double delta)
    {
        if (!BaseInputable || playerData == null) return;
        if (playerData.FaceLeft) sprite2D_body.SetScale(new Vector2(1, 1));
        else sprite2D_body.SetScale(new Vector2(-1, 1));

        Vector2I coord = new(
            (int)Mathf.Floor(GetGlobalMousePosition().X / 16),
            (int)Mathf.Floor(GetGlobalMousePosition().Y / 16)
        );

        if (Input.IsActionPressed("breakblock") && playerData != null && OpeningInventoryNode == null &&
            OvrCanvasLayer.GetChildCount() == 0)
        {
            OnMouseLeftClick(coord, delta);
            LastFramIsLeft = true;
        }
        else
        {
            LastFramIsLeft = false;
            if (ActionProcess.State == PlayerAction.BreakBlock && ActionProcess.ProcessTime > 0)
                ActionProcess.ProcessTime = 0;
        }

        if (Input.IsActionPressed("placeblock") && playerData != null && OpeningInventoryNode == null &&
            OvrCanvasLayer.GetChildCount() == 0)
        {
            if (!OnMouseRightClick(coord, delta))
            {
            }

            LastFramIsRight = true;
        }
        else
        {
            if (ActionProcess.State == PlayerAction.UseItem && ActionProcess.ProcessTime > 0)
            {
                ActionProcess.ProcessTime = 0;
            }
            LastFramIsRight = false;
        }


        if (ActionProcess.ProcessTime > 0)
        {
            if (animationPlayer_other.CurrentAnimation != "break")
                animationPlayer_other.Play("break");
        }
        else animationPlayer_other.Stop();
    }


    public override void _PhysicsProcess(double delta)
    {
        if (playerData == null || world == null) return;
        if (!world.HasTileMap(playerData.ChunkCoord)) return;

        Vector2I chunkCoord = Position.ToVector2I().MathFloor(Chunk.Size);
        UpdateCursor();
        AntiOnChunkUnload(chunkCoord);
        InputHandle(delta);
        UpdateViews();
    }

    private void UpdateViews()
    {
        if (playerData.State == PlayerState.Dead)
        {
            if (OvrCanvasLayer.GetChildCount() == 0)
            {
                var dv = PackedSceneDeadView.Instantiate<DeadView>();
                dv.Ready += () => { dv.SetPlayerDead(this); };
                OvrCanvasLayer.AddChild(dv);
            }
        }

        if (playerData.State == PlayerState.Live && OvrCanvasLayer.GetChildCount() > 0)
        {
            foreach (var node in OvrCanvasLayer.GetChildren())
            {
                if (node is DeadView dv)
                {
                    dv.QueueFree();
                }
            }
        }
    }

    //鼠标左键
    private void OnMouseLeftClick(Vector2I coord, double delta)
    {
        var pos = coord;
        if (pos.X > playerData.Coord.X)
            playerData.FaceLeft = false;
        if (pos.X < playerData.Coord.X)
            playerData.FaceLeft = true;


        var pos0 = new Vector3I(pos.X, pos.Y, 0);
        var pos1 = new Vector3I(pos.X, pos.Y, 1);
        var block1 = world.Service.ChunkService.GetBlock(pos0);
        var block2 = world.Service.ChunkService.GetBlock(pos1);
        if (block1 == null || block2 == null)
        {
            ActionProcess.Reset();
            return;
        }

        Vector3I finalpos;
        BlockData InterfaceBlock;
        if (Input.IsKeyPressed(Key.Shift))
        {
            finalpos = pos1;
            InterfaceBlock = block2;
        }
        else
        {
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
        }

        LastFramLayer = finalpos.Z;

        if (InterfaceBlock.IsMeta("air"))
        {
            ActionProcess.Reset();
            return;
        }

        if (ActionProcess.State == PlayerAction.BreakBlock)
        {
            if (ActionProcess.ProcessTime >= ActionProcess.FinalTime)
            {
                var bbe = new PlayerBreakblockEvent()
                {
                    world = world,
                    Player = playerData,
                    Position = ActionProcess.Position,
                };
                world.Service.PlayerService.Events.BreakBlock(bbe);
                ActionProcess.ProcessTime = 0;
                ActionProcess.FinalTime = 2;
                return;
            }
            else if (ActionProcess.ProcessTime > 0)
            {
                ActionProcess.ProcessTime += (float)delta;
                if (ActionProcess.Position != finalpos)
                    ActionProcess.Reset();
                return;
            }
        }

        if (!world.Service.ChunkService.CheckIsCloseBlock(finalpos))
        {
            var meta = InterfaceBlock.BlockMeta;
            float efficiency = 1f;
            float gap = meta.BreakLevel;

            var durable = playerData.Inventory.GetToolBarItem()?.GetComponent<ItemDurableComponent>();
            if (durable != null)
            {
                string tag = InterfaceBlock.GetTag("type");
                if ((tag != null && durable.HasTag(tag)) || durable.HasTag("any"))
                    efficiency = 1f + durable.Efficiency * 0.25f;
                else
                {
                    GD.Print("not has tag");
                }

                gap = meta.BreakLevel - durable.ToolLevel;
            }

            //工具等级差距过大
            if (gap > 1) return;
            ActionProcess.State = PlayerAction.BreakBlock;
            ActionProcess.FinalTime = InterfaceBlock.BlockMeta.Rigidity / efficiency;
            ActionProcess.Position = finalpos;
            ActionProcess.ProcessTime += (float)delta;
            if (playerData.Mode == 1) ActionProcess.FinalTime = 0;
        }
        else
        {
            Cursor.Frame = 1;
        }
    }

    //鼠标右键
    private bool OnMouseRightClick(Vector2I coord, double delta)
    {
        var targetpos = new Vector3I(coord.X, coord.Y, 0);
        bool coercivePlace = false;
        if (Input.IsKeyPressed(Key.Shift))
        {
            coercivePlace = true;
            targetpos = new Vector3I(coord.X, coord.Y, 0);
        }

        var e = new PlayerPlaceBlockEvent()
        {
            world = world,
            Player = playerData,
            Position = targetpos,
            CoerciveBackGroundPlace = coercivePlace,
            IsCollideWithPlayer = IsInRange(targetpos.X * 16, targetpos.Y * 16)
        };
        //连续放置时优先放置上一次图层
        if (LastFramIsRight)
            e.CoerciveLayer = LastFramLayer;

        if (world.Service.PlayerService.Events.PlaceBlock(e))
            LastFramLayer = e.PlaceLayerResult;
        else if (!LastFramIsRight)
            world.Service.PlayerService.Events.InterfaceBlock(new InterfaceBlockEvent()
            {
                world = world,
                Player = playerData,
                Position = targetpos,
            });

        //使用物品和放置方块不冲突，如果物品在放置方块时被消耗了，这个正常是不会触发的。
        var handitem = playerData.Inventory.GetToolBarItem();
        if (handitem != null)
        {
            var cmp = handitem.GetComponent<ItemUsefulComponent>();
            if (cmp != null)
            {
                if (ActionProcess.State == PlayerAction.UseItem)
                {
                    ActionProcess.ProcessTime += (float)delta;
                    if (ActionProcess.ProcessTime >= ActionProcess.FinalTime)
                    {
                        var puie = new PlayerUseItemEvent()
                        {
                            world = world,
                            Player = playerData,
                            UseItemStack = handitem,
                            Position = ActionProcess.Position
                        };
                        world.Service.PlayerService.Events.UseItem(puie);
                        ActionProcess.Reset();
                        return true;
                    }
                }
                else
                {
                    ActionProcess.State = PlayerAction.UseItem;
                    ActionProcess.FinalTime = cmp.UseTime;
                    ActionProcess.ProcessTime = 0;
                    return true;
                }
            }
        }

        //ActionProcess.State = PlayerAction.None;
        return false;
    }

    //处理输入
    private void InputHandle(double delta)
    {
        if (playerData == null) return;
        if (!BaseInputable) return;

        if (playerData.Mode == 0)
        {
            var velocity = Velocity;
            if (!IsOnFloor() && (!playerData.Fly.Value || !Stop))
            {
                velocity += GetGravity() * (float)delta;
            }

            Velocity = velocity;
            MoveAndSlide();
        }

        if (!Inputable) return;


        bool AnyMove = false;

        for (int i = 1; i < 10; i++)
            if (Input.IsKeyPressed(i + Key.Key0))
            {
                playerData.Inventory.ToolBarIndex = (byte)(i - 1);
                if (world.Service is ClientWorldService wcs)
                    world.Service.PlayerInventoryServiceNode.RpcId(
                        1,
                        nameof(PlayerInventoryServiceNode.SetHandSlot),
                        playerData.Name, playerData.Inventory.ToolBarIndex
                    );
                if (ActionProcess.ProcessTime > 0)
                    ActionProcess.ProcessTime = 0;
            }


        if (Input.IsActionJustPressed("roller_up"))
        {
            playerData.Inventory.ToolBarIndex -= 1;
            if (playerData.Inventory.ToolBarIndex < 0) playerData.Inventory.ToolBarIndex = 8;
            if (world.Service is ClientWorldService wcs)
                world.Service.PlayerInventoryServiceNode.RpcId(
                    1,
                    nameof(PlayerInventoryServiceNode.SetHandSlot),
                    playerData.Name, playerData.Inventory.ToolBarIndex
                );
            if (ActionProcess.ProcessTime > 0)
                ActionProcess.ProcessTime = 0;
        }

        if (Input.IsActionJustPressed("roller_down"))
        {
            playerData.Inventory.ToolBarIndex += 1;
            if (playerData.Inventory.ToolBarIndex > 8) playerData.Inventory.ToolBarIndex = 0;
            if (world.Service is ClientWorldService wcs)
                world.Service.PlayerInventoryServiceNode.RpcId(
                    1,
                    nameof(PlayerInventoryServiceNode.SetHandSlot),
                    playerData.Name, playerData.Inventory.ToolBarIndex
                );
            if (ActionProcess.ProcessTime > 0)
                ActionProcess.ProcessTime = 0;
        }

        if (Input.IsActionJustPressed("e") && OvrCanvasLayer.GetChildCount() == 0)
        {
            if (OpeningInventoryNode != null)
            {
                world.Service.PlayerService.Events.CloseInventory(world.Service, playerData.Name);
                world.PlayerNode.playerData.OpeningBlockInventory = false;
                RemoveChild(OpeningInventoryNode);
                OpeningInventoryNode = null;
            }
            else
                world.Service.PlayerService.Events.OpenInventory(world, "PlayerInventory");
        }

        if (Input.IsActionJustPressed("OpenOperatingMenu"))
        {
            if (OvrCanvasLayer.GetChildCount() == 0)
            {
                var menu = GD.Load<PackedScene>("res://tscn/Menu/operating_menu.tscn");
                OvrCanvasLayer.AddChild(menu.Instantiate<OperatingMenu>());

                if (OpeningInventoryNode != null)
                {
                    world.Service.PlayerService.Events.CloseInventory(world.Service, playerData.Name);
                    world.PlayerNode.playerData.OpeningBlockInventory = false;
                    RemoveChild(OpeningInventoryNode);
                    OpeningInventoryNode = null;
                }
            }
            else
            {
                var children = OvrCanvasLayer.GetChildren();
                foreach (var child in children)
                {
                    OvrCanvasLayer.RemoveChild(child);
                    child.QueueFree();
                }
            }
        }

        ///丢弃单个物品
        if (Input.IsActionJustPressed("drop_item"))
        {
            world.Service.PlayerService.Events.DropItem(world.Service, playerData.Name);
        }

        //丢弃所有物品
        if (Input.IsActionJustPressed("drop_all_item"))
        {
            world.Service.PlayerService.Events.DropAllItem(world.Service, playerData.Name);
        }

        if (playerData.Mode == 0)
        {
            Vector2 velocity = Velocity;
            if (Input.IsActionPressed("ui_accept") && (IsOnFloor() || playerData.Fly.Value))
            {
                velocity.Y = JumpVelocity;
                AnyMove = true;
            }

            Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
            if (direction != Vector2.Zero)
            {
                velocity.X = direction.X * playerData.MoveSpeed.Value;
                if (direction.Y > 0) velocity.Y += direction.Y * playerData.MoveSpeed.Value;
                AnyMove = true;
            }
            else
            {
                velocity.X = Mathf.MoveToward(Velocity.X, 0, playerData.MoveSpeed.Value);
            }

            if (velocity.X > 0) playerData.FaceLeft = false;
            else if (velocity.X < 0) playerData.FaceLeft = true;

            Velocity = velocity * playerData.Resistance.Value;
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
            if (direction != Vector2.Zero) AnyMove = true;
            if (direction.X > 0) playerData.FaceLeft = false;
            else if (direction.X < 0) playerData.FaceLeft = true;
            Position += direction * 16;
        }


        if (AnyMove)
        {
            if (animationPlayer_move.CurrentAnimation != "moveing")
                animationPlayer_move.Play("moveing");
        }
        else
        {
            if (animationPlayer_move.CurrentAnimation != "ide")
            {
                animationPlayer_move.Play("RESET");
                animationPlayer_move.Play("ide");
            }
        }


        if (TEST_MODE)
        {
            if (Input.IsActionJustPressed("TEST_1"))
            {
            }

            if (Input.IsActionJustPressed("F1") && BaseInputable)
                playerData.Mode = playerData.Mode == 0 ? 1 : 0;
            if (Input.IsActionJustPressed("F2") && BaseInputable)
                DebugView.DEBUG = !DebugView.DEBUG;
            if (Input.IsActionJustPressed("F3") && BaseInputable)
            {
                MoreInfo = !MoreInfo;
                Label_DEBUG_Left.Visible = MoreInfo;
                Label_DEBUG_Right.Visible = MoreInfo;
            }

            if (Input.IsActionJustPressed("F5") && BaseInputable)
            {
                playerData.State = PlayerState.Dead;
                // Position = new Vector2(new Random().Next(100000), Position.Y);
                // OnMoveToChunk?.Invoke();
            }
        }
    }

    //防止卡在未加载区块里面
    private void AntiOnChunkUnload(Vector2I coord)
    {
        // if (world == null || !world.Service.ChunkService.Chunks.ContainsKey(coord))
        // {
        //     Stop = true;
        // }
        // else if (playerData.Mode == 0)
        //     Stop = false;
    }

    //更新挖掘进度条
    private void UpdateCursor()
    {
        Label_PlayerName.Text = Profile.Name;

        Vector2I coord = new(
            (int)Mathf.Floor(GetGlobalMousePosition().X / 16),
            (int)Mathf.Floor(GetGlobalMousePosition().Y / 16)
        );


        float progress = ActionProcess.ProcessTime / ActionProcess.FinalTime;
        if (progress > 1) progress = 1;

        if (ActionProcess.State == PlayerAction.BreakBlock)
        {
            Use_Process.Visible = false;
            if (ActionProcess.ProcessTime > 0)
            {
                Cursor.Visible = true;
                Cursor.Frame = 1 + (int)(8 * progress);
            }
            else Cursor.Frame = 0;

            Cursor.GlobalPosition = new Vector2(coord.X, coord.Y) * 16;
        }

        if (ActionProcess.State == PlayerAction.UseItem)
        {
            Cursor.Visible = false;
            if (ActionProcess.ProcessTime > 0)
            {
                Use_Process.Visible = true;
                Use_Process.Frame = (int)(14 * progress);
            }
            else
            {
                Use_Process.Visible = false;
                Use_Process.Frame = 0;
            }

            Use_Process.GlobalPosition = new Vector2(coord.X, coord.Y) * 16;
        }


        var blockBack = world.Service.ChunkService.GetBlock(new Vector3I(coord.X, coord.Y, 0));
        var BlockFont = world.Service.ChunkService.GetBlock(new Vector3I(coord.X, coord.Y, 1));
        if (blockBack == null || BlockFont == null) return;
        if (!BlockFont.IsMeta("air"))
        {
            BlockInfoView.SetBlockData(BlockFont);
        }
        else
        {
            BlockInfoView.SetBlockData(blockBack);
        }
    }

    public override void _Ready()
    {
        hotBar.PlayerNode = this;
    }

    //检查是否和碰撞箱重叠
    private bool IsInRange(int x, int y, float w = 16f, float h = 16f)
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