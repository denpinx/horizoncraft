using Godot;

namespace Horizoncraft.script.Entity;

/// <summary>
/// 通过实现这个接口，就可以统一存储和管理各种被注册的Godot节点
/// </summary>
public interface IEntityNode
{
    public World World { get; set; }
    public EntityData Entity { get; set; }
    public int Id { get; set; }
    public Vector2 LastPosition { get; set; }
    public Node2D GetNode();
}