using Godot;

namespace horizoncraft.script.Utility;

public static class NodeUtility
{
    private static void SetSameLevelVisible(Node2D Node, bool visible)
    {
        var p = Node.GetParent();
        foreach (var child in p.GetChildren())
        {
            if (child is CanvasItem ci && ci != Node)
            {
                ci.Visible = visible;
            }
        }
    }
}