using System.Collections.Generic;
using Godot;

namespace horizoncraft.script.Net;

//客户端和服务器都共用这个对象
public class DataSubscription
{
    public string Name;
    public HashSet<long> subscriber = new();
    public Dictionary<string, Node> ContextNode = new();
    public bool IsHost = true;
    public Server server;

    public DataSubscription(Server server, bool isHost = true)
    {
        this.IsHost = isHost;
        this.server = server;
    }

    public void AddContextNode(string name, Node node)
    {
        if (ContextNode.ContainsKey(name))
        {
            GD.PrintErr("上下文节点重复添加!");
        }
        else
        {
            ContextNode.Add(name, node);
        }
    }

    //订阅
    public void Subscribe(long id)
    {
        if (IsHost && !subscriber.Contains(id))
        {
            subscriber.Add(id);
        }
        else
        {
            server.RpcId(id, "RpcPool_Server_Subscribe", Name, id);
        }
    }

    //取消订阅
    public void Unsubscribe(long id)
    {
        if (IsHost && subscriber.Contains(id))
        {
            subscriber.Remove(id);
        }
        else
        {
            server.RpcId(id, "RpcPool_Server_Unsubscribe", Name, id);
        }
    }

    //同步数据给客户端
    public void SyncDataToCilent(Server server)
    {
        foreach (long id in subscriber)
        {
            server.RpcId(id, "RpcPool_Client_Recive", Name, GetBytes(id));
        }
    }

    //同步数据给服务端
    public virtual void SyncDataToServer(Server server)
    {
    }

    //客户端接收
    public virtual void CilentReciveData(byte[] bytes)
    {
    }

    //服务端接收
    public virtual void HostReciveData(byte[] bytes)
    {
    }

    //准备订阅者所需资源
    public virtual byte[] GetBytes(long id)
    {
        return [];
    }
}