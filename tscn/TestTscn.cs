using Godot;
using System.Collections.Generic;
using horizoncraft.script;
using horizoncraft.script.Components;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using horizoncraft.script.Components.TestComponents;
using horizoncraft.script.Expand;
using horizoncraft.script.I18N;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Struct;

public partial class TestTscn : Node2D
{
    public override void _Ready()
    {
        GD.Print("---单元测试---");
        ChunkTest(1000);
        GetAllStructsTest(1000);
        LambdaTest_Flat(10000);
        LambdaTest_DeepMode(10000);
    }

    public void LambdaTest_DeepMode(int count)
    {
        var func = LambdaCreater.CreateLambda<TestComponent>("TestComponent", new Dictionary<string, object>()
        {
            ["Name"] = "TestComponent",
            ["Age"] = 12345,
            ["Tags"] = new Dictionary<string, string>()
            {
                ["test_1"] = "value_1",
                ["test_2"] = "value_2",
            },
        }, true);
        _ = func();
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int i = 0; i < count; i++)
        {
            _ = func();
        }

        stopwatch.Stop();
        Log(
            $"{nameof(LambdaTest_DeepMode)} 生成{count} 个对象,总耗时{stopwatch.Elapsed.TotalMilliseconds} ms , {stopwatch.Elapsed.TotalMicroseconds} μs");
    }

    public void LambdaTest_Flat(int count)
    {
        //lambda已经在构建时被预热一次了,这里构建耗时170ms,不作为测试标准
        var func = LambdaCreater.CreateLambda<Component>("TickComponent", new Dictionary<string, object>()
        {
            ["name"] = "TickComponent",
        }, true);

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int i = 0; i < count; i++)
        {
            _ = func();
        }

        stopwatch.Stop();
        Log(
            $"{nameof(LambdaTest_Flat)} 生成{count} 个对象,总耗时{stopwatch.Elapsed.TotalMilliseconds} ms , {stopwatch.Elapsed.TotalMicroseconds} μs");

        var f = () => new TickComponent() { Name = "TickComponent" };
        stopwatch.Restart();
        for (int i = 0; i < count; i++)
        {
            _ = f();
        }

        stopwatch.Stop();
        Log($"原生Lambda构建 {count} 个对象,总耗时{stopwatch.Elapsed.TotalMilliseconds} ms");

        stopwatch.Restart();
        for (int i = 0; i < count; i++)
        {
            _ = new TickComponent() { Name = "TickComponent" };
        }

        stopwatch.Stop();
        Log($"原生直接构建 {count} 个对象,总耗时{stopwatch.Elapsed.TotalMilliseconds} ms");
        Log("");
    }

    public void SpawnTreeTest(int count)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        Random random = new Random();
        for (int i = 0; i < count; i++)
        {
            BlockStructManager.GetStruct("oak_tree", i, 0, 0, random);
        }   

        stopwatch.Stop();
        Log($"单线程生成 {count} 结构{stopwatch.Elapsed.TotalMilliseconds} ms");
        Log($"平均耗时{stopwatch.Elapsed.TotalMilliseconds / count} ms");
    }

    public void GetAllStructsTest(int count)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int i = 0; i < count; i++)
            WorldGenerator.GetAllStructs(i, 0);
        Log($"单线程生成 {count} 结构{stopwatch.Elapsed.TotalMilliseconds} ms");
        Log($"平均耗时{stopwatch.Elapsed.TotalMilliseconds / count} ms");
    }

    public void ChunkTest(int count)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int i = 0; i < count; i++)
        {
            Chunk chunk = new Chunk(i, 0);
            WorldGenerator.Generator(chunk);
        }

        stopwatch.Stop();
        Log($"单线程生成 {count} 区块耗时{stopwatch.Elapsed.TotalMilliseconds} ms");
        Log($"平均耗时{stopwatch.Elapsed.TotalMilliseconds / count} ms");

        stopwatch.Restart();
        Parallel.For(0, count, i =>
        {
            Chunk chunk = new Chunk(i, 0);
            WorldGenerator.Generator(chunk);
        });

        stopwatch.Stop();
        Log($"多线程生成 {count} 区块耗时{stopwatch.Elapsed.TotalMilliseconds} ms");
        Log($"平均耗时{stopwatch.Elapsed.TotalMilliseconds / count} ms");
    }

    private void Log(string msg)
    {
        GD.Print($"[cell test] {msg}");
    }
}