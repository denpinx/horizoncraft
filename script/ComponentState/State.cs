using System.Collections.Generic;
using MemoryPack;

namespace Horizoncraft.script.ComponentState;

/// <summary>
/// 状态容器,存储和限制容器内部的值的
/// </summary>
/// <typeparam name="T"></typeparam>
[MemoryPackable]
public partial class State<T> where T : IItem
{
    /// <summary>
    /// 物流模式，用来约束当前容器能否被自动输入或被自动拉取输出
    /// </summary>
    public LogisticsMode LogisticsMode = LogisticsMode.Any;

    /// <summary>
    /// 过滤器模式
    /// </summary>
    public StateFilter FilterMode = StateFilter.Any;

    /// <summary>
    /// 过滤器集合，如果是Any模式就不需要，用来约束输入的流体的
    /// </summary>
    public List<string> FilterItems=[];

    /// <summary>
    /// 当前值
    /// </summary>
    public required T Value;

    /// <summary>
    /// 数量限制
    /// </summary>
    public required int Max;

    /// <summary>
    /// 尝试压入
    /// </summary>
    /// <param name="input">对象</param>
    /// <returns>完全被压入</returns>
    public bool TryPush(T input)
    {
        if (FilterMode == StateFilter.BlackList)
            if (FilterItems != null && FilterItems.Contains(input.Name))
                return false;


        if (FilterMode == StateFilter.WhiteList)
            if (FilterItems == null || !FilterItems.Contains(input.Name))
                return false;


        if (Value.Name != input.Name)
            if (Value.Amount > 0)
                return false;
            else
                Value.Name = input.Name;

        var count = input.Amount + Value.Amount;
        if (count > Max)
        {
            input.Amount = count - Max;
            Value.Amount = Max;
            return false;
        }

        Value.Amount = count;
        input.Amount = 0;
        return true;
    }
}

/// <summary>
/// 过滤器模式
/// </summary>
public enum StateFilter
{
    Any,
    WhiteList,
    BlackList
}

/// <summary>
/// 物流模式
/// </summary>
public enum LogisticsMode
{
    Any,
    Input,
    Output
}