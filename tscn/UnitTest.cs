using System;
using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using Horizoncraft.script;
using Horizoncraft.script.Net;
using Horizoncraft.script.UnitTest;
using Horizoncraft.script.UnitTest.Tests;

public partial class UnitTest : Control
{
    private PackedScene TestLabel = GD.Load<PackedScene>("res://tscn/testtscn/TestLabel.tscn");
    public Dictionary<string, UnitTestResult> LastTestResults;
    public Dictionary<string, UnitTestResult> TestResults = new Dictionary<string, UnitTestResult>();
    public Dictionary<string, IUnitTest> UnitTestItems = new();

    [Export] public VBoxContainer Tabel_Test_Name;
    [Export] public VBoxContainer Tabel_Test_UseTime;
    [Export] public VBoxContainer Tabel_Test_LastUseTime;
    [Export] public VBoxContainer Tabel_Test_Result;

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
        Load();
        GD.Print($"[{nameof(UnitTest)}] StartTest");
        foreach (var item in UnitTestItems)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var result = item.Value.Start(this, item.Key);
            GD.Print($"[{nameof(UnitTest)}] Result: {result}");
            stopwatch.Stop();

            var Result = new UnitTestResult()
            {
                TestName = item.Key,
                UseTime = stopwatch.Elapsed.TotalMicroseconds,
                TestTime = DateTime.Now,
                ResultText = result,
            };
            TestResults.Add(item.Key, Result);
        }

        UpdateUi();
        Save();
    }

    public void UpdateUi()
    {
        foreach (var item in TestResults.Values)
        {
            double last_test_time = Double.NaN;
            if (LastTestResults != null && LastTestResults.TryGetValue(item.TestName, out var last))
            {
                last_test_time = last.UseTime;
            }

            var l1 = TestLabel.Instantiate<Label>();
            var l2 = TestLabel.Instantiate<Label>();
            var l3 = TestLabel.Instantiate<Label>();
            var l4 = TestLabel.Instantiate<Label>();
            l1.Text = item.TestName;
            l2.Text = $"{item.UseTime}μs,{(item.UseTime / 1000):F2}ms,{(item.UseTime / 1000 / 1000):F2}s";
            l3.Text = $"{last_test_time}μs,{(last_test_time / 1000):F2}ms,{(last_test_time / 1000 / 1000):F2}s";
            l4.Text = item.ResultText;
            Tabel_Test_Name.AddChild(l1);
            Tabel_Test_UseTime.AddChild(l2);
            Tabel_Test_LastUseTime.AddChild(l3);
            Tabel_Test_Result.AddChild(l4);
        }
    }

    public void Load()
    {
        if (!DirAccess.DirExistsAbsolute("test"))
            DirAccess.MakeDirAbsolute("test");

        if (FileAccess.FileExists("test/result.data"))
        {
            var file = FileAccess.Open("test/result.data", FileAccess.ModeFlags.Read);
            var bytpe = file.GetBuffer((long)file.GetLength());
            file.Close();
            var list = ByteTool.FromBytes<Dictionary<string, UnitTestResult>>(bytpe);
            this.LastTestResults = list;
        }
    }

    public void Save()
    {
        var bytes = ByteTool.ToBytes(TestResults);
        var file = FileAccess.Open("test/result.data", FileAccess.ModeFlags.Write);
        file.StoreBuffer(bytes);
        file.Close();
    }
}