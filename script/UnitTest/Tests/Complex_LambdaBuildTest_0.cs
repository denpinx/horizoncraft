using System;
using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Components;
using Horizoncraft.script.Components.TestComponents;

namespace Horizoncraft.script.UnitTest.Tests;

public class Complex_LambdaBuildTest_0 : UnitTestItem<Func<TestComponent>>
{
    public override bool MatchResult(Func<TestComponent> result)
    {
        if (result == null) return false;
        var output = result();
        if (output == null) return false;
        if (output.Name != "TestComponent") return false;
        if (output.Age != 12345) return false;
        if (!output.Tags.ContainsKey("test_1")) return false;
        else if (output.Tags["test_1"] != "value_1") return false;
        if (!output.Tags.ContainsKey("test_2")) return false;
        else if (output.Tags["test_2"] != "value_2") return false;

        return true;
    }

    protected override Func<TestComponent> StartTest(Node node)
    {
        var func = LambdaCreater.CreateLambda<TestComponent>("TestComponent", new Dictionary<string, object>()
        {
            ["name"] = "TestComponent",
            ["age"] = 12345,
            ["tags"] = new Dictionary<string, string>()
            {
                ["test_1"] = "value_1",
                ["test_2"] = "value_2",
            },
        }, true);
        _ = func();
        return func;
    }
}