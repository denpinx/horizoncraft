namespace horizoncraft.script.Net.SyncDatas;

public class ChatSync : DataSubscription
{
    public ChatSync(Server server, bool isHost = true) : base(server, isHost)
    {
    }
}