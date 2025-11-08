using System.Diagnostics;
using Godot;

namespace Horizoncraft.script.UnitTest.Tests;

public class ComponentBuildSpeedTest : UnitTestItem<object>
{
    protected override object StartTest(Node node)
    {
        const int trycount = 1000000;
        Stopwatch stopwatch = new Stopwatch();
        foreach (var meta in Materials.BlockMetas.Values)
        {
            if (meta.Components.Count > 0)
            {
                stopwatch.Restart();

                foreach (var cmp in meta.Components)
                {
                    for (int index = 0; index < trycount; index++)
                    {
                        cmp();
                    }
                }

                stopwatch.Stop();
                GD.Print(
                    $"#{meta.Name,-24} 方块组件创建 {trycount,-8}次 : total {stopwatch.Elapsed.TotalMicroseconds,-8} μs");
            }
        }

        foreach (var meta in Materials.ItemMetas.Values)
        {
            if (meta.Components.Count > 0)
            {
                stopwatch.Restart();

                foreach (var cmp in meta.Components)
                {
                    for (int index = 0; index < trycount; index++)
                    {
                        cmp();
                    }
                }

                stopwatch.Stop();
                GD.Print(
                    $"#{meta.Name,-24} 物品组件创建 {trycount,-8}次 : total {stopwatch.Elapsed.TotalMilliseconds,-8} μs");
            }
        }

        return null;
    }
}