using Godot;
using horizoncraft.script.Chat;
using horizoncraft.script.Net;

namespace horizoncraft.script.rpc;

public partial class MessageServiceNode : Node
{
    World World;

    public MessageServiceNode(World world)
    {
        this.World = world;
        this.Name = nameof(MessageServiceNode);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void CilentReciveMessage(byte[] data)
    {
        var msg = ByteTool.FromBytes<MessageData>(data);
        World.Service.MessageService.AddMessage(msg);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ServerReciveMessage(byte[] data)
    {
        var msg = ByteTool.FromBytes<MessageData>(data);
        World.Service.MessageService.UserInputMessage(msg);
    }
}