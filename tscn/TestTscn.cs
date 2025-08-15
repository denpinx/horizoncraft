using Godot;
using System.Collections.Generic;
using horizoncraft.script;
using horizoncraft.script.Components;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using horizoncraft.script.WorldControl;

public partial class TestTscn : Node2D
{
    public override void _Ready()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int i = 0; i < 1000; i++)
        {
            Chunk chunk = new Chunk(i, 0);
            WorldGenerator.Generator(chunk);
        }

        stopwatch.Stop();
        GD.Print($"单线程生成 1000 区块耗时{stopwatch.ElapsedMilliseconds} ms");
        GD.Print($"平均耗时{stopwatch.ElapsedMilliseconds / 1000} ms");


        stopwatch.Restart();
        Parallel.For(0, 1000, i =>
        {
            Chunk chunk = new Chunk(i, 0);
            WorldGenerator.Generator(chunk);
        });

        stopwatch.Stop();
        GD.Print($"多线程生成 1000 区块耗时{stopwatch.ElapsedMilliseconds} ms");
        GD.Print($"平均耗时{stopwatch.ElapsedMilliseconds / 1000} ms");
    }
}