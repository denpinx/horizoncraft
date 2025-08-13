using Godot;
using System.Collections.Generic;
using horizoncraft.script;
using horizoncraft.script.Components;
using System;

public partial class TestTscn : Node2D
{
    public override void _Ready()
    {
        Dictionary<string,object> dict = new Dictionary<string,object>()
        {
            ["Name"] = "GrassSpread",
            ["Max"] = 20,
            ["Current"] = 0
        };
        LambdaCreater.Register<TickComponent>();
        var lmd = LambdaCreater.CreateLambda("TickComponent", dict);
        var TC = lmd() as TickComponent;
        GD.Print("lmd Name:", TC.Name);
        GD.Print("lmd Max:", TC.Max);
        GD.Print("lmd Current:", TC.Current);
    }   


}
