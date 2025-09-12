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
    private const bool TEST_MODE = true;
    public static System.Collections.Generic.Dictionary<string, Func<string>> GetInformation = new();
    public static LocalProfile Profile;

    public PlayerBreakProcess BreakProcess = new PlayerBreakProcess();
    public const float JumpVelocity = -200.0f;
    public Action OnMoveToChunk;
    public PlayerData playerData;
    public bool Inputable = true;
    public bool MoreInfo = false;
    public bool Stop = false;
    public World world;


    //
    AnimationPlayer animationPlayer_other;
    AnimationPlayer animationPlayer_move;
    CollisionShape2D collisionShape2D;
    public CanvasLayer OvrCanvasLayer;
    public InventoryNode ShowView;
    public RayCast2D RayCast;
    public Timer Timer_Tick;
    Label Label_DEBUG_Right;
    Label Label_DEBUG_Left;
    Label Label_PlayerName;
    Sprite2D sprite2D_body;
    public Sprite2D Cursor;
    public HotBar hotBar;
    Camera2D camera2d;
    //


    private bool LastFramIsLeft = false;
    private bool LastFramIsRight = false;

    //private Vector2I LastFramPosition;

    public override void _Process(double delta)
    {
        if (!Inputable || playerData == null) return;
        if (playerData.FaceLeft) sprite2D_body.SetScale(new Vector2(1, 1));
        else sprite2D_body.SetScale(new Vector2(-1, 1));

        Vector2I coord = new(
            (int)Mathf.Floor(GetGlobalMousePosition().X / 16),
            (int)Mathf.Floor(GetGlobalMousePosition().Y / 16)
        );

        if (Input.IsActionPressed("breakblock") && playerData != null && ShowView == null &&
            OvrCanvasLayer.GetChildCount() == 0)
        {
            OnMouseLeftClick(coord, delta);
        }
        else
        {
            if (BreakProcess.ProcessTime > 0) BreakProcess.ProcessTime = 0;
        }

        if (Input.IsActionPressed("placeblock") && playerData != null && ShowView == null &&
            OvrCanvasLayer.GetChildCount() == 0)
        {
            OnMouseRightClick(coord, delta);
        }


        if (BreakProcess.ProcessTime > 0)
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
        UpdatePlayerPosition();
        AntiOnChunkUnload(chunkCoord);
        InputHandle(delta);
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
            BreakProcess.Reset();
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


        if (InterfaceBlock.IsMeta("air"))
        {
            BreakProcess.Reset();
            return;
        }

        if (BreakProcess.ProcessTime >= BreakProcess.FinalTime)
        {
            var bbe = new PlayerBreakblockEvent()
            {
                world = world,
                Player = playerData,
                Position = BreakProcess.Position,
            };
            world.Service.PlayerService.Events.BreakBlock(bbe);
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
            if (!world.Service.ChunkService.CheckIsCloseBlock(finalpos))
            {
                var meta = InterfaceBlock.BlockMeta;
                float efficiency = 1f;
                float gap = meta.BreakLevel;

                var durable = playerData.Inventory.GetItemInHand()?.GetComponent<ItemDurableComponent>();
                if (durable != null)
                {
                    string tag = InterfaceBlock.GetTag("type");
                    if ((tag != null && durable.HasTag(tag)) || durable.HasTag("any"))
                        efficiency = 1f + durable.Efficiency * 0.25f;
                    gap = meta.BreakLevel - durable.ToolLevel;
                }

                //工具等级差距过大
                if (gap > 1) return;

                BreakProcess.FinalTime = InterfaceBlock.BlockMeta.Rigidity / efficiency;
                BreakProcess.Position = finalpos;
                BreakProcess.ProcessTime += (float)delta;
                if (playerData.Mode == 1) BreakProcess.FinalTime = 0;
            }
            else
            {
                Cursor.Frame = 1;
            }
        }
    }

    //鼠标右键
    private void OnMouseRightClick(Vector2I coord, double delta)
    {
        var targetpos = new Vector3I(coord.X, coord.Y, 0);
        bool coercive = false;
        if (Input.IsKeyPressed(Key.Shift))
        {
            coercive = true;
            targetpos = new Vector3I(coord.X, coord.Y, 0);
        }


        if (
            world.Service.PlayerService.Events.PlaceBlock(new()
            {
                world = world,
                Player = playerData,
                Position = targetpos,
                coercive = coercive,
                IsCollide = IsInRange(targetpos.X * 16, targetpos.Y * 16)
            }))
        {
        }
        else
        {
            world.Service.PlayerService.Events.InterfaceBlock(new InterfaceBlockEvent()
            {
                world = world,
                Player = playerData,
                Position = targetpos,
            });
        }
    }

    //处理输入
    private void InputHandle(double delta)
    {
        if (!Inputable) return;
        bool AnyMove = false;
        if (playerData != null)
        {
            for (int i = 1; i < 10; i++)
                if (Input.IsKeyPressed(i + Key.Key0))
                {
                    playerData.Inventory.HandSlot = (byte)(i - 1);
                    if (world.Service is ClientWorldService wcs)
                        world.Service.PlayerInventoryServiceNode.RpcId(
                            1,
                            nameof(PlayerInventoryServiceNode.SetHandSlot),
                            playerData.Name, playerData.Inventory.HandSlot
                        );
                    if (BreakProcess.ProcessTime > 0)
                        BreakProcess.ProcessTime = 0;
                }


            if (Input.IsActionJustPressed("roller_up"))
            {
                playerData.Inventory.HandSlot -= 1;
                if (playerData.Inventory.HandSlot < 0) playerData.Inventory.HandSlot = 8;
                if (world.Service is ClientWorldService wcs)
                    world.Service.PlayerInventoryServiceNode.RpcId(
                        1,
                        nameof(PlayerInventoryServiceNode.SetHandSlot),
                        playerData.Name, playerData.Inventory.HandSlot
                    );
                if (BreakProcess.ProcessTime > 0)
                    BreakProcess.ProcessTime = 0;
            }

            if (Input.IsActionJustPressed("roller_down"))
            {
                playerData.Inventory.HandSlot += 1;
                if (playerData.Inventory.HandSlot > 8) playerData.Inventory.HandSlot = 0;
                if (world.Service is ClientWorldService wcs)
                    world.Service.PlayerInventoryServiceNode.RpcId(
                        1,
                        nameof(PlayerInventoryServiceNode.SetHandSlot),
                        playerData.Name, playerData.Inventory.HandSlot
                    );
                if (BreakProcess.ProcessTime > 0)
                    BreakProcess.ProcessTime = 0;
            }

            if (Input.IsActionJustPressed("e") && OvrCanvasLayer.GetChildCount() == 0)
            {
                if (ShowView != null)
                {
                    world.Service.PlayerService.Events.CloseInventory(world.Service, playerData.Name);
                    world.PlayerNode.playerData.OpeningBlockInventory = false;
                    RemoveChild(ShowView);
                    ShowView = null;
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

                    if (ShowView != null)
                    {
                        world.Service.PlayerService.Events.CloseInventory(world.Service, playerData.Name);
                        world.PlayerNode.playerData.OpeningBlockInventory = false;
                        RemoveChild(ShowView);
                        ShowView = null;
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
        }

        if (playerData.Mode == 0)
        {
            Vector2 velocity = Velocity;
            if (!IsOnFloor() && (!playerData.Fly.Value || !Stop))
            {
                velocity += GetGravity() * (float)delta;
            }

            if (Input.IsActionPressed("ui_accept") && (IsOnFloor() || playerData.Fly.Value))
            {
                velocity.Y = JumpVelocity;
                AnyMove = true;
            }

            Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
            if (direction != Vector2.Zero)
            {
                velocity.X = direction.X * playerData.MoveSpeed.Value;
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
            Position += direction * 64;
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
                //生成生物

                var data = new EntityData()
                {
                    Name = "item_entity",
                    Owned = PlayerNode.Profile.Name,
                    Position = new(GetGlobalMousePosition().X, GetGlobalMousePosition().Y),
                    Components = new List<Component>()
                    {
                        new ItemEntityComponent()
                        {
                            Name = "ItemEntityComponent",
                            ItemStack = Materials.Dictionary_ItemMetas["grass"].GetItemStack()
                        }
                    }
                };
                world.Service.EntityService.AddEntityData(data);
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
                foreach (var item in Materials.ItemMetas)
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

    //防止卡在未加载区块里面
    private void AntiOnChunkUnload(Vector2I coord)
    {
        if (world == null || !world.Service.ChunkService.Chunks.ContainsKey(coord))
        {
            Stop = true;
        }
        else if (playerData.Mode == 0)
            Stop = false;
    }

    //更新玩家节点位置到Data
    private void UpdatePlayerPosition()
    {
        var pos = new System.Numerics.Vector2(Position.X, Position.Y);
        if (playerData.LastPosition != pos) playerData.Update = true;
        playerData.LastPosition = Position.ToSystemVector2();
        playerData.Position = pos;
        if (world.Service.PlayerService.Players.TryGetValue(playerData.Name, out var data))
        {
            data.Position = playerData.Position;
            data.Update = playerData.Update;
            data.FaceLeft = playerData.FaceLeft;
        }
    }

    //更新挖掘进度条
    private void UpdateCursor()
    {
        float progress = BreakProcess.ProcessTime / BreakProcess.FinalTime;
        if (progress > 1) progress = 1;
        if (BreakProcess.ProcessTime > 0)
            Cursor.Frame = 1 + (int)(8 * progress);
        else Cursor.Frame = 0;
        Cursor.GlobalPosition = new Vector2(BreakProcess.Position.X, BreakProcess.Position.Y) * 16;
    }

    public override void _Ready()
    {
        var loadingMenu = GetNode<LoadingMenu>("OvrCanvasLayer/LoadingMenu");

        Label_DEBUG_Left = GetNode<Label>("CanvasLayer/Control/Label_DEBUG_Left");
        Label_DEBUG_Right = GetNode<Label>("CanvasLayer/Control/Label_DEBUG_Right");
        animationPlayer_move = GetNode<AnimationPlayer>("AnimationPlayer_Move");
        animationPlayer_other = GetNode<AnimationPlayer>("AnimationPlayer_Other");
        collisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
        OvrCanvasLayer = GetNode<CanvasLayer>("OvrCanvasLayer");
        Label_PlayerName = GetNode<Label>("Label_PlayerName");
        hotBar = GetNode<HotBar>("CanvasLayer/HotBar");
        sprite2D_body = GetNode<Sprite2D>("Body");
        Timer_Tick = GetNode<Timer>("Timer_Tick");
        camera2d = GetNode<Camera2D>("Camera2D");
        Cursor = GetNode<Sprite2D>("Cursor");

        if (playerData != null)
            playerData.PlayerNode = this;
        hotBar.PlayerNode = this;
        loadingMenu.playerNode = this;

        //Label_PlayerName.Text = Profile.Name;
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