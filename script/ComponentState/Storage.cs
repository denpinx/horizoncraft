using System.Collections.Generic;
using MemoryPack;

namespace Horizoncraft.script.ComponentState;

/// <summary>
/// 存储容器，一个容器可以有多个State。
/// </summary>
[MemoryPackable]
public partial class Storage<T> where T : IItem, new()
{
    public List<State<T>> States = [];

    public bool TryInput(T input, bool All = false)
    {
        foreach (var item in States)
        {
            if (!All && (item.LogisticsMode == LogisticsMode.Output))
                continue;

            if (item.TryPush(input))
                break;
        }

        if (input.Amount == 0)
            return true;
        return false;
    }

    public bool TryTakeFluid(string Name, int Amount, out T result, bool Any = false)
    {
        foreach (var item in States)
        {
            if (!Any && (item.LogisticsMode == LogisticsMode.Output))
                continue;
            if (Name != "")
                if (item.Value.Name != Name)
                    continue;

            if (item.Value.Amount >= Amount)
            {
                item.Value.Amount -= Amount;
                result = new T
                {
                    Amount = Amount,
                    Name = item.Value.Name,
                };
                return true;
            }
        }

        result = default(T);
        return false;
    }

    public Storage<T> AddState(State<T> state)
    {
        States.Add(state);
        return this;
    }

    public bool IsEmpty()
    {
        foreach (var item in States)
        {
            if (item.Value.Amount > 0)
                return false;
        }

        return true;
    }
}