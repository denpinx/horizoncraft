using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Expand;
using Horizoncraft.script.NewProxy.player;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.Services.player;

public class SinglePlayerService : PlayerServiceBase
{
    public SinglePlayerService(World world) : base(world)
    {
    }

    public override void OnPlayerRespawn(PlayerData playerData)
    {
        World.PlayerNode.Position = playerData.Position.ToGodotVector2();
    }
}