using Godot;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Components.EntityComponents;
using horizoncraft.script.Entity;
using horizoncraft.script.Inventory;
using horizoncraft.script.WorldControl.Service;
using HorizonCraft.script.WorldControl.Service;

namespace HorizonCraft.tscn.Entity;

public partial class ItemEntity : RigidBody2D, IEntityNode
{
    public World World { get; set; }
    public EntityData Entity { get; set; }
    public int Id { get; set; }
    public Vector2 LastPosition { get; set; }
    public bool Moved { get; set; }

    //节点
    TextureRect _itemTexture_;
    Label _itemLabel_;
    Area2D _area2D_;
    Area2D _area2DFar_;
    AnimationPlayer _animationPlayer_;

    //自身属性
    double _cooldown_ = 0.5f;
    Player _player_;
    Player _far_player_;

    public override void _Ready()
    {
        base._Ready();
        World = (World)GetParent();

        _itemTexture_ = GetNode<TextureRect>("TextureRect");
        _animationPlayer_ = GetNode<AnimationPlayer>("AnimationPlayer");
        _itemLabel_ = GetNode<Label>("Label");
        _area2D_ = GetNode<Area2D>("Area2D");
        _area2DFar_ = GetNode<Area2D>("Area2D_Far");
        _area2D_.BodyEntered += OnEnter;
        _area2D_.BodyExited += OnExit;
        _area2DFar_.BodyEntered += FarPlayerEnter;
        _area2DFar_.BodyExited += FarPlayerExit;
        _animationPlayer_.Play("ide");
    }

    public void FarPlayerEnter(Node2D node)
    {
        if (node is Player player)
        {
            _far_player_ = player;
        }
    }

    public void FarPlayerExit(Node2D node)
    {
        if (node is Player player)
        {
            _far_player_ = null;
        }
    }

    public void OnEnter(Node2D node)
    {
        if (node is Player player)
        {
            _player_ = player;
            return;
        }

        if (node is ItemEntity itemEntity && itemEntity != this)
        {
            var self = Entity.GetComponent<ItemEntityComponent>();
            var target = itemEntity.Entity.GetComponent<ItemEntityComponent>();
            if (target == null) return;
            if (self == null) return;

            if (target?.ItemStack == null)
            {
                World.WorldService.EntityService.RemoveEntityData(itemEntity.Entity.Uuid);
                return;
            }
            else
            {
                if (target.ItemStack.Id == self.ItemStack.Id)
                {
                    int space = self.ItemStack.GetItemMeta().MaxAmount - self.ItemStack.Amount;

                    if (GetInstanceId() < itemEntity.GetNode().GetInstanceId())
                    {
                        if (space > 0)
                            if (space >= target.ItemStack.Amount)
                            {
                                self.ItemStack.Amount += target.ItemStack.Amount;
                                World.WorldService.EntityService.RemoveEntityData(itemEntity.Entity.Uuid);
                                return;
                            }
                            else
                            {
                                self.ItemStack.Amount += space;
                                target.ItemStack.Amount -= space;
                            }
                    }
                }
            }
        }
    }

    public void OnExit(Node2D node)
    {
        if (node is Player)
        {
            _player_ = null;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (World.HasTileMap(Entity.ChunkCoord))
            Freeze = false;
        else
            Freeze = true;


        if (_cooldown_ > 0) _cooldown_ -= delta;
        else _cooldown_ = 0;

        if (_cooldown_ == 0 && _player_ != null)
        {
            var cmp = Entity.GetComponent<ItemEntityComponent>();
            if (cmp != null && _player_.playerData.Inventory.TryAddItem(cmp.ItemStack))
            {
                World.WorldService.EntityService.RemoveEntityData(Entity.Uuid);
                return;
            }
        }


        if (_far_player_ != null)
        {
            Vector2 direction = (_far_player_.GlobalPosition - GlobalPosition).Normalized();

            Vector2 attractVelocity = direction * 40f;
            Vector2 newVelocity = LinearVelocity;
            newVelocity.X += attractVelocity.X;
            newVelocity.X = Mathf.Clamp(newVelocity.X, -100f, 100f);
            LinearVelocity = newVelocity;
        }

        var selfcmp = Entity.GetComponent<ItemEntityComponent>();
        if (selfcmp == null) return;

        if (
            World.WorldService is WorldHostService || //服务端
            World.WorldService is WorldSingleService || //单机
            Entity.Owned == Player.Profile.Name //该实体的更新责任在当前玩家手中
        )
        {
            Entity.Position.X = GlobalPosition.X;
            Entity.Position.Y = GlobalPosition.Y;
        }
        else if (World.WorldService is WorldClientService)
        {
            if (Entity.Owned != Player.Profile.Name)
            {
                GlobalPosition = new Vector2(Entity.Position.X, Entity.Position.Y);
            }
        }

        if (LastPosition != GlobalPosition) Entity.Update = true;
        LastPosition = GlobalPosition;


        if (selfcmp.ItemStack != null)
        {
            _itemTexture_.Visible = true;
            _itemTexture_.Texture = selfcmp.ItemStack.GetItemMeta().GetTexture();
            //_itemLabel_.Text = $"*{selfcmp.ItemStack.Amount}";
            _itemLabel_.Text = $"所属{Entity.Owned},更新状态{Entity.Update}";
        }
        else
        {
            _itemTexture_.Visible = false;
        }
    }

    public override void _EnterTree()
    {
        GlobalPosition = new(Entity.Position.X, Entity.Position.Y);
    }


    public Node2D GetNode()
    {
        return this;
    }
}