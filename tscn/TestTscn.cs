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
using horizoncraft.script.resource;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Struct;

public partial class TestTscn : Node2D
{
    public override void _Ready()
    {
        //ChunkTest(1000);
        //GetAllStructsTest(200);
        // var bmr =GD.Load<BlockMetaResource>("res://resources/block/stone.tres");
        // GD.Print(bmr.Components[0].cmps.ToCsharp());
        
        //测不准！
        LambdaTest(10);


    }
    
    public void LambdaTest(int count)
    {

        var func = LambdaCreater.CreateLambda("TickComponent", new Dictionary<string, object>()
        {
            ["Name"] = "TickComponent",
        });
        //GD.Print($"V2 构建耗时{stopwatch.ElapsedMilliseconds} ms");
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int i = 0; i < count; i++)
        {
            _ = func();
        }

        stopwatch.Stop();
        GD.Print($"V2 {count} 个,总耗时{stopwatch.ElapsedMilliseconds} ms");
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