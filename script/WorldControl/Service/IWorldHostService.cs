namespace HorizonCraft.script.WorldControl.Service;

/// <summary>
/// 服务器同步功能
/// </summary>
public interface IWorldHostService
{
    ///<summary>同步玩家</summary>
    public void SyncPlayers();
    ///<summary>同步区块</summary>
    public void SyncChunks();
}