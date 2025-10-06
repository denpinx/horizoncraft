using horizoncraft.script.Chat;
using horizoncraft.script.Net;
using horizoncraft.script.rpc;

namespace horizoncraft.script.Services.message;

public class ClientMessageService : MessageServiceBase
{
    public ClientMessageService(World world) : base(world)
    {
    }

    public override void UserInputMessage(MessageData message)
    {
        base.UserInputMessage(message);
        World.Service.MessageServiceNode.RpcId(1, nameof(MessageServiceNode.ServerReciveMessage),
            ByteTool.ToBytes(message));
    }
}