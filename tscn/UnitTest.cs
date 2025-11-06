using Godot;
using System.Collections.Generic;
using horizoncraft.script;
using horizoncraft.script.UnitTest;
using horizoncraft.script.UnitTest.Tests;

public partial class UnitTest : Node2D
{
    public Dictionary<string, IUnitTest> UnitTestItems = new();

    public override void _Ready()
    {
        _ = Materials.BlockMetas.Count;
            
        UnitTestItems.Add("简单组件Lambda构建测试-忽略大小写", new Simple_LambdaBuildTest_0());
        UnitTestItems.Add("复杂组件Lambda构建测试-大小写匹配", new Simple_LambdaBuildTest_1());
        UnitTestItems.Add("嵌套组件Lambda构建测试-忽略大小写", new Complex_LambdaBuildTest_0());
        UnitTestItems.Add("区块构建速度测试", new ChunkGeneratorTest());
        UnitTestItems.Add("组件构建速度测试", new ComponentBuildSpeedTest());
        StartTest();
    }

    public void StartTest()
    {
        GD.Print($"[{nameof(UnitTest)}] StartTest");
        foreach (var item in UnitTestItems)
        {
            var result = item.Value.Start(this, item.Key);
            GD.Print(result);
        }
    }
}