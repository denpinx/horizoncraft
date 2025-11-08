using Horizoncraft.script.Expand;

namespace Horizoncraft.script.Entity;

/// <summary>
/// 客户端实体行为
/// </summary>
public class ClientEntityBehavior : EntityBehaviorBase
{
    public override void Process(IEntityNode entityNode, double delta)
    {
        if (entityNode.Entity.Owned != PlayerNode.Profile.Name)
            entityNode.GetNode().GlobalPosition = entityNode.Entity.Position.ToGodotVector2();
        else
        {
            entityNode.Entity.Position = entityNode.GetNode().GlobalPosition.ToSystemVector2();
        }

        CheckUpdate(entityNode, delta);
    }
}