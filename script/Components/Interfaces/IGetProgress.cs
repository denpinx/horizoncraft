namespace horizoncraft.script.Components.Interfaces;
/// <summary>
/// 获取进度值
/// </summary>
public interface IGetProgress
{
    public ProgressValue GetProgress();
}

public struct ProgressValue
{
    public string Name;
    public int Value;
    public int Max;
}