using System.Diagnostics;

namespace horizoncraft.script.Entity;

/// <summary>
/// 实体行为基础
/// </summary>
public class EntityBehaviorBase
{
    public virtual void Process(IEntityNode entityNode, double delta)
    {
        var node = entityNode.GetNode();
        entityNode.Entity.Position.X = node.GlobalPosition.X;
        entityNode.Entity.Position.Y = node.GlobalPosition.Y;
        CheckUpdate(entityNode, delta);
    }

    public void CheckUpdate(IEntityNode entityNode, double delta)
    {
        if (entityNode.Entity.LastPosition != entityNode.Entity.Position)
        {
            entityNode.Entity.Update = true;
        }

        entityNode.Entity.LastPosition = entityNode.Entity.Position;
    }
}