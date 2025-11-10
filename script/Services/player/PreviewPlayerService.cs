using Godot;
using Horizoncraft.script;
using Horizoncraft.script.Expand;
using Horizoncraft.script.NewProxy.player;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.script.Services.player;

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
            Name = PlayerNode.Profile.Name,
            State = PlayerState.Live
        };
        Players.TryAdd(playerData.Name, playerData);
        return true;
    }

    public override void ProcessPlayerState()
    {
        //补丁一
        if (World.PlayerNode.playerData != null)
        {
            World.PlayerNode.playerData.Position = World.PlayerNode.Position.ToSystemVector2();
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