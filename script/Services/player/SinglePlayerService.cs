using Godot;
using horizoncraft.script;
using horizoncraft.script.NewProxy.player;
using horizoncraft.script.WorldControl;

namespace HorizonCraft.script.Services.player;

public class SinglePlayerService : PlayerServiceBase
{
    public SinglePlayerService(World world) : base(world)
    {
    }
}