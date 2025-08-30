using MemoryPack;

namespace horizoncraft.script;

[MemoryPackable]
public partial class ConfigSet<T>
{
    public T Value;
    public T Default;

    public void Reset()
    {
        Value = default;
    }

    public bool IsDefault()
    {
        return Value.Equals(Default);
    }
}