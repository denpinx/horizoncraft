using Godot.Collections;

namespace Horizoncraft;

public static class RunConfig
{
    public static RunMode Mode = RunMode.Client;
    public static string WorldName;
    public static long WorldSeed;
    public static string Ip;
    public static int Port;

    public static Dictionary ToDictionary()
    {
        return new Dictionary()
        {
            ["world-name"] = WorldName,
            ["world-seed"] = WorldSeed.ToString(),
            ["ip"] = Ip,
            ["port"] = Port
        };
    }
}

public enum RunMode
{
    Server,
    Client,
}