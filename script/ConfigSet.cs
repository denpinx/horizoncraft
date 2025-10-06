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
    [MemoryPackConstructor]
    public ConfigSet()
    {
        
    }
    public ConfigSet(T value)
    {
        this.Value = value;
        this.Default = value;
    }
}