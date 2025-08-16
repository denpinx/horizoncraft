using Godot;
using horizoncraft.script;
using horizoncraft.script.WorldControl;

namespace HorizonCraft.script.WorldControl.Service;

/// <summary>
/// 后端存储
/// </summary>
public interface IWorldService
{
    ///<summary>初始化数据</summary>
    public bool Init();

    ///<summary>计算计划载区块坐标</summary>
    public void UpdateLoadChunkCoords();

    public void ProcessChunkUnloadQueue();

    /// <summary>
    /// 处理区块加载队列
    /// </summary>
    public void ProcessChunkLoadQueue();

    /// <summary>
    /// 处理玩家加载队列
    /// </summary>
    public void ProcessPlayerLoadQueue();


    /// <summary>
    /// 获取玩家
    /// </summary>
    /// <param name="name">玩家名称</param>
    /// <param name="playerdata">返回的玩家数据</param>
    /// <returns>是否成功获取</returns>
    public bool GetPlayer(string name, out PlayerData playerdata);

    /// <summary>
    /// 保存玩家数据
    /// </summary>
    /// <param name="playerData">玩家数据</param>
    public void SavePlayer(PlayerData playerData);

    /// <summary>
    /// 保存区块
    /// </summary>
    /// <param name="chunk">要保存的区块</param>
    public void SaveChunk(Chunk chunk);

    ///<summary>
    /// 保存所有数据
    /// </summary>
    public void Save();
}