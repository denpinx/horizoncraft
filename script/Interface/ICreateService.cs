namespace horizoncraft.script.Interface;

/// <summary>
/// 创建对象
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
public interface ICreateService<out T1, in T2>
{
    public T1 Create(T2 b);
}

public interface ICreateService<out T>
{
    public T Create();
}