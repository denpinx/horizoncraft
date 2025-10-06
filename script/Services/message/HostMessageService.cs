using horizoncraft.script.Chat;
using horizoncraft.script.Net;
using horizoncraft.script.rpc;

namespace horizoncraft.script.Services.message;

public class HostMessageService : MessageServiceBase
{
    public HostMessageService(World world) : base(world)
    {
    }

    protected override void SyncMessage(MessageData message)
    {
        foreach (var player in World.Service.PlayerService.Players.Values)
        {
            if (message.PlayerName == player.Name) continue;
            World.Service.MessageServiceNode.RpcId(player.PeerId, nameof(MessageServiceNode.CilentReciveMessage),
                ByteTool.ToBytes(message));
        }
    }
}