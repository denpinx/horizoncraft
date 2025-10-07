using Godot;
using horizoncraft.script;
using horizoncraft.script.NewProxy.player;
using horizoncraft.script.WorldControl;

namespace HorizonCraft.script.Services.player;

public class PreviewPlayerService : PlayerServiceBase
{
    public PreviewPlayerService(World world) : base(world)
    {
    }

    /// <summary>
    /// 预览模式,只创建新的玩家实列
    /// </summary>
    /// <param name="name"></param>
    /// <param name="playerData"></param>
    /// <returns></returns>
    /// 
    public override bool GetPlayerOrLoad(string name, out PlayerData playerData)
    {
        playerData = new PlayerData()
        {
            Name = PlayerNode.Profile.Name
        };
        Players.TryAdd(playerData.Name, playerData);
        return true;
    }

    public override PlayerData LoadPlayer(string name)
    {
        return new PlayerData()
        {
            Name = PlayerNode.Profile.Name
        };
    }

    public override void ProcessPlayerState()
    {
        foreach (var player in Players.Values)
        {
            player.State = PlayerState.Live;
        }
    }

    public override void SaveAll()
    {
    }

    public override void SavePlayer(PlayerData player)
    {
    }

    public override bool TrySearchSpawn(PlayerData player, Vector2I position)
    {
        player.Position = new(position.X * 16, position.Y * 16);
        return true;
    }
}