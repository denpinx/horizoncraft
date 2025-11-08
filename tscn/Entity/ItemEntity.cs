using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Components.EntityComponents;
using Horizoncraft.script.Entity;
namespace HorizonCraft.tscn.Entity;

public partial class ItemEntity : RigidBody2D, IEntityNode
{
    public World World { get; set; }
    public EntityData Entity { get; set; }
    public int Id { get; set; }
    public Vector2 LastPosition { get; set; }
    public bool Moved { get; set; }

    //节点
    [Export] TextureRect _itemTexture_;
    [Export] Label _itemLabel_;
    [Export] Area2D _area2D_;
    [Export] Area2D _area2DFar_;
    [Export] AnimationPlayer _animationPlayer_;

    //自身属性
    double _cooldown_ = 0.5f;
    PlayerNode _playerNode;
    PlayerData _farPlayerNode;

    public override void _Ready()
    {
        base._Ready();
        World = (World)GetParent();
        _area2DFar_.BodyEntered += FarPlayerEnter;
        _area2DFar_.BodyExited += FarPlayerExit;
        _animationPlayer_.Play("ide");
    }

    public void FarPlayerEnter(Node2D node)
    {
        if (node is PlayerNode player)
        {
            _farPlayerNode = player.playerData;
        }

        if (node is PlayerSnapshot psn)
        {
            _farPlayerNode = psn.playerData;
        }
    }

    public void FarPlayerExit(Node2D node)
    {
        if (node is PlayerNode or PlayerSnapshot)
        {
            _farPlayerNode = null;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (World.HasTileMap(Entity.ChunkCoord))
            Freeze = false;
        else
            Freeze = true;

        if (!Freeze) World.Service.EntityBehavior.Process(this, delta);

        if (_farPlayerNode != null)
        {
            Vector2 direction = (_farPlayerNode.Position_v2 - GlobalPosition).Normalized();
            Vector2 attractVelocity = direction * 40f;
            Vector2 newVelocity = LinearVelocity;
            newVelocity.X += attractVelocity.X;
            newVelocity.X = Mathf.Clamp(newVelocity.X, -100f, 100f);
            LinearVelocity = newVelocity;
        }

        var selfcmp = Entity.GetComponent<ItemEntityComponent>();
        if (selfcmp == null) return;
        if (selfcmp.ItemStack != null)
        {
            _itemTexture_.Visible = true;
            _itemTexture_.Texture = selfcmp.ItemStack.GetItemMeta().GetTexture();
            _itemLabel_.Text = $"*{selfcmp.ItemStack.Amount}";
            //_itemLabel_.Text = $"[所属{Entity.Owned}] [更新状态{Entity.Update}]";
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