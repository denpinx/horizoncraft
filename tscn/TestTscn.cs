using Godot;
using System.Collections.Generic;
using horizoncraft.script;
using horizoncraft.script.Components;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using horizoncraft.script.Expand;
using horizoncraft.script.I18N;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Struct;

public partial class TestTscn : Node2D
{
    public override void _Ready()
    {
        GD.Print(LanguageManage.Trprefix("air"));
        //ChunkTest(1000);
        // LambdaTest(100);
        // LambdaTest(100000);
        // LambdaTest(1000000);
        // LambdaTest(10000000);
    }

    public void LambdaTest(int count)
    {
        //lambda已经在构建时被预热一次了,这里构建耗时170ms,不作为测试标准
        var func = LambdaCreater.CreateLambda("TickComponent", new Dictionary<string, object>()
        {
            ["name"] = "TickComponent",
        },true);
        
        
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int i = 0; i < count; i++)
        {
            _ = func();
        }

        stopwatch.Stop();
        GD.Print($"LambdaCreater 生成{count} 个对象,总耗时{stopwatch.ElapsedMilliseconds} ms");

        var f = () => new TickComponent() { Name = "TickComponent" };
        stopwatch.Restart();
        for (int i = 0; i < count; i++)
        {
            _ = f();
        }

        stopwatch.Stop();
        GD.Print($"原生Lambda构建 {count} 个对象,总耗时{stopwatch.ElapsedMilliseconds} ms");
        
        stopwatch.Restart();
        for (int i = 0; i < count; i++)
        {
            _ = new TickComponent() { Name = "TickComponent" };
        }

        stopwatch.Stop();
        GD.Print($"原生直接构建 {count} 个对象,总耗时{stopwatch.ElapsedMilliseconds} ms");
        GD.Print("");
    }

    public void SpawnTreeTest(int count)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        Random random = new Random();
        for (int i = 0; i < count; i++)
        {
            StructManage.GetStruct("oak_tree", i, 0, 0, random);
        }

        stopwatch.Stop();
        GD.Print($"单线程生成 {count} 结构{stopwatch.ElapsedMilliseconds} ms");
        GD.Print($"平均耗时{stopwatch.ElapsedMilliseconds / count} ms");
    }

    public void GetAllStructsTest(int count)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int i = 0; i < count; i++)
            WorldGenerator.GetAllStructs(i, 0);
        GD.Print($"单线程生成 {count} 结构{stopwatch.ElapsedMilliseconds} ms");
        GD.Print($"平均耗时{stopwatch.ElapsedMilliseconds / count} ms");
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
        GD.Print($"单线程生成 {count} 区块耗时{stopwatch.ElapsedMilliseconds} ms");
        GD.Print($"平均耗时{stopwatch.ElapsedMilliseconds / count} ms");

        stopwatch.Restart();
        Parallel.For(0, count, i =>
        {
            Chunk chunk = new Chunk(i, 0);
            WorldGenerator.Generator(chunk);
        });

        stopwatch.Stop();
        GD.Print($"多线程生成 {count} 区块耗时{stopwatch.ElapsedMilliseconds} ms");
        GD.Print($"平均耗时{stopwatch.ElapsedMilliseconds / count} ms");
    }
}