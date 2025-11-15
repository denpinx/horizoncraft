using System;
using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Components;

namespace Horizoncraft.script.UnitTest.Tests;

public class Simple_LambdaBuildTest_0 : UnitTestItem<Func<ItemComponent>>
{
    public override bool MatchResult(Func<ItemComponent> result)
    {
        if (result == null) return false;
        var outputObject = result();
        if (outputObject == null) return false;
        if (outputObject.Drive != "Simple_LambdaBuildTest_0") return false;
        return true;
    }

    protected override Func<ItemComponent> StartTest(Node node)
    {
        var func = LambdaCreater.CreateLambda<ItemComponent>("ItemComponent", new Dictionary<string, object>()
        {
            ["drive"] = "Simple_LambdaBuildTest_0",
        }, true);
        _ = func();
        return func;
    }
}