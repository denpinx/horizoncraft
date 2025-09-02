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
    public static System.Collections.Generic.Dictionary<string, Func<string>> GetInformation = new();

    public Action OnMoveToChunk;
    public PlayerData playerData;

    public World world;

    //public const float Speed = 300.0f;
    public const float JumpVelocity = -200.0f;
    public bool Inputable = true;
    public bool MoreInfo = false;
    //[Export] public bool fly = true;

    public bool Stop = false;

    //
    Camera2D camera2d;
    public Timer Timer_Tick;
    Label Label_DEBUG_Left;
    Label Label_DEBUG_Right;
    Label Label_PlayerName;
    CollisionShape2D collisionShape2D;
    AnimationPlayer animationPlayer_move;
    AnimationPlayer animationPlayer_other;
    Sprite2D sprite2D_body;
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

            if (pos.X > playerData.Coord.X)
                sprite2D_body.Scale = new Vector2(-1, 1);
            else if (pos.X < playerData.Coord.X)
                sprite2D_body.Scale = new Vector2(1, 1);


            var pos0 = new Vector3I(pos.X, pos.Y, 0);
            var pos1 = new Vector3I(pos.X, pos.Y, 1);
            var block1 = world.WorldService.GetBlock(pos0);
            var block2 = world.WorldService.GetBlock(pos1);
            if (block1 == null || block2 == null)
            {
                BreakProcess.Reset();
                return;
            }

            Vector3I finalpos;
            Blockdata InterfaceBlock;
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
                if (!world.WorldService.CheckIsCloseBlock(finalpos))
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
        else
        {
            if (BreakProcess.ProcessTime > 0) BreakProcess.ProcessTime = 0;
        }

        if (Input.IsActionPressed("placeblock") && playerData != null && ShowView == null)
        {
            var targetpos = new Vector3I(Mousecoord.X, Mousecoord.Y, 0);
            bool coercive = false;
            if (Input.IsKeyPressed(Key.Shift))
            {
                coercive = true;
                targetpos = new Vector3I(Mousecoord.X, Mousecoord.Y, 0);
            }


            if (
                world.WorldService.PlaceBlock(playerData, targetpos, coercive,
                    IsInRange(targetpos.X * 16, targetpos.Y * 16)))
            {
            }
            else
            {
                world.WorldService.InterfaceBlock(playerData, targetpos);
            }
        }


        if (BreakProcess.ProcessTime > 0)
        {
            if (animationPlayer_other.CurrentAnimation != "break")
                animationPlayer_other.Play("break");
        }
        else
        {
            animationPlayer_other.Stop();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (playerData == null) return;
        if (playerData.Name != Player.Profile.Name) return;


        Vector2I Mcoord = World.MathFloor((Vector2I)GetGlobalMousePosition(), 16);
        Cursor.GlobalPosition = Mcoord * 16;

        float f1 = BreakProcess.ProcessTime / BreakProcess.FinalTime;
        if (f1 > 1) f1 = 1;
        if (BreakProcess.ProcessTime > 0)
            Cursor.Frame = 1 + (int)(8 * f1);
        else Cursor.Frame = 0;

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

        InputHandle(delta);
    }

    public void InputHandle(double delta)
    {
        if (!Inputable) return;
        bool AnyMove = false;
        if (playerData != null)
        {
            for (int i = 1; i < 10; i++)
                if (Input.IsKeyPressed(i + Key.Key0))
                {
                    playerData.Inventory.HandSlot = (byte)(i - 1);
                    if (world.WorldService is WorldClientService wcs)
                        world.RpcId(1, "SetHandSlot", playerData.Name, playerData.Inventory.HandSlot);
                    if (BreakProcess.ProcessTime > 0)
                        BreakProcess.ProcessTime = 0;
                }


            if (Input.IsActionJustPressed("roller_up"))
            {
                playerData.Inventory.HandSlot -= 1;
                if (playerData.Inventory.HandSlot < 0) playerData.Inventory.HandSlot = 8;
                if (world.WorldService is WorldClientService wcs)
                    world.RpcId(1, "SetHandSlot", playerData.Name, playerData.Inventory.HandSlot);
                if (BreakProcess.ProcessTime > 0)
                    BreakProcess.ProcessTime = 0;
            }

            if (Input.IsActionJustPressed("roller_down"))
            {
                playerData.Inventory.HandSlot += 1;
                if (playerData.Inventory.HandSlot > 8) playerData.Inventory.HandSlot = 0;
                if (world.WorldService is WorldClientService wcs)
                    world.RpcId(1, "SetHandSlot", playerData.Name, playerData.Inventory.HandSlot);
                if (BreakProcess.ProcessTime > 0)
                    BreakProcess.ProcessTime = 0;
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

            if (velocity.X > 0) sprite2D_body.Scale = new Vector2(-1, 1);
            else if (velocity.X < 0) sprite2D_body.Scale = new Vector2(1, 1);

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
            if (direction.X > 0) sprite2D_body.Scale = new Vector2(-1, 1);
            else if (direction.X < 0) sprite2D_body.Scale = new Vector2(1, 1);
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
                    Owned = Player.Profile.Name,
                    Position = new(GetGlobalMousePosition().X, GetGlobalMousePosition().Y),
                    Components = new List<Component>()
                    {
                        new ItemEntityComponent()
                        {
                            ItemStack = Materials.Dictionary_ItemMetas["grass"].GetItemStack()
                        }
                    }
                };
                world.WorldService.EntityService.AddEntityData(data);
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


    public override void _Ready()
    {
        sprite2D_body = GetNode<Sprite2D>("Body");
        animationPlayer_move = GetNode<AnimationPlayer>("AnimationPlayer_Move");
        animationPlayer_other = GetNode<AnimationPlayer>("AnimationPlayer_Other");
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