namespace Horizoncraft.script.ComponentState;

public interface IStorage<T> where T : IItem, new()
{
    public Storage<T> GetStorage();
}