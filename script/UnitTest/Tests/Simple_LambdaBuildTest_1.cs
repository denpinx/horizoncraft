using System;
using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Components;
using Horizoncraft.script.Components.Item;

namespace Horizoncraft.script.UnitTest.Tests;

public class Simple_LambdaBuildTest_1 : UnitTestItem<Func<ToolComponent>>
{
    public override bool MatchResult(Func<ToolComponent> result)
    {
        if (result == null) return false;
        var outputObject = result();
        if (outputObject == null) return false;
        if (outputObject.Drive != "Simple_LambdaBuildTest_1") return false;
        if (outputObject.Max != 1) return false;
        if (outputObject.Value != 2) return false;
        if (outputObject.ToolLevel != 3) return false;
        if (outputObject.Efficiency != 4) return false;
        if (!outputObject.Tag.Contains("Simple_LambdaBuildTest_1")) return false;
        return true;
    }

    protected override Func<ToolComponent> StartTest(Node node)
    {
        var func = LambdaCreater.CreateLambda<ToolComponent>("ToolComponent",
            new Dictionary<string, object>()
            {
                ["Drive"] = "Simple_LambdaBuildTest_1",
                ["Max"] = 1,
                ["Value"] = 2,
                ["ToolLevel"] = 3,
                ["Efficiency"] = 4,
                ["Tag"] = new List<string>() { "Simple_LambdaBuildTest_1" },
            });
        _ = func();
        return func;
    }
}