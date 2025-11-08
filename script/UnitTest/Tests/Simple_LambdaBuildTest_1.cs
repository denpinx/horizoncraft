using System;
using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Components;
using Horizoncraft.script.Components.Item;

namespace Horizoncraft.script.UnitTest.Tests;

public class Simple_LambdaBuildTest_1 : UnitTestItem<Func<ItemDurableComponent>>
{
    public override bool MatchResult(Func<ItemDurableComponent> result)
    {
        if (result == null) return false;
        var outputObject = result();
        if (outputObject == null) return false;
        if (outputObject.Name != "Simple_LambdaBuildTest_1") return false;
        if (outputObject.Max != 1) return false;
        if (outputObject.Value != 2) return false;
        if (outputObject.ToolLevel != 3) return false;
        if (outputObject.Efficiency != 4) return false;
        if (outputObject.Tag != "Simple_LambdaBuildTest_1") return false;
        return true;
    }

    protected override Func<ItemDurableComponent> StartTest(Node node)
    {
        var func = LambdaCreater.CreateLambda<ItemDurableComponent>("ItemDurableComponent",
            new Dictionary<string, object>()
            {
                ["Name"] = "Simple_LambdaBuildTest_1",
                ["Max"] = 1,
                ["Value"] = 2,
                ["ToolLevel"] = 3,
                ["Efficiency"] = 4,
                ["Tag"] = "Simple_LambdaBuildTest_1",
            });
        _ = func();
        return func;
    }
}