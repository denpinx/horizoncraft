using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.UnitTest.Tests;

public class ChunkGeneratorTest : UnitTestItem<object>
{
    const int count = 1000;

    protected override object StartTest(Node node)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int i = 0; i < count; i++)
        {
            Chunk chunk = new Chunk(i, 0);
            WorldGenerator.Generator(chunk);
        }

        stopwatch.Stop();
        GD.Print($"单线程生成 {count} 区块耗时{stopwatch.Elapsed.TotalMilliseconds} ms");
        GD.Print($"平均耗时{stopwatch.Elapsed.TotalMilliseconds / (float)count} ms");
        stopwatch.Restart();
        return null;
    }
}