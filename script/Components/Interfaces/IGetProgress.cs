namespace horizoncraft.script.Components.Interfaces;

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