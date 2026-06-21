using Horizoncraft.script.Chat;
using Horizoncraft.script.Net;
using Horizoncraft.script.rpc;

namespace Horizoncraft.script.Services.Message;

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