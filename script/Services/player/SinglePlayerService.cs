using Godot;
using horizoncraft.script;
using horizoncraft.script.Expand;
using horizoncraft.script.NewProxy.player;
using horizoncraft.script.WorldControl;

namespace HorizonCraft.script.Services.player;

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