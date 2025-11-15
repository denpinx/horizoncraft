using System;
using Godot;
using Horizoncraft.script.Components.BlockComponents;

namespace Horizoncraft.script.RenderSystem.render;

public class FluidPipeRender() : RenderSystem("fluid_pipe_render")
{
    public Transform2D GetTransform(Vector2I targetPosition, float angle)
    {
        Vector2 center = new Vector2(16, 16) / 2.0f;
        float rotationRad = Mathf.DegToRad(angle);
        Transform2D transform = Transform2D.Identity;
        transform = transform.Translated(-center);
        transform = transform.Rotated(rotationRad);
        transform = transform.Translated(new Vector2(targetPosition.X + center.X, targetPosition.Y + center.Y));
        return transform;
    }

    public override void OnDraw(RenderContext context)
    {
        var meta = context.Block.BlockMeta;
        var cmp = context.Block.GetComponent<ReactiveComponent>();
        var pos = context.pos_v2 * 16;
        //连接贴图
        var texutre_connect = meta.ExpandTextures["fluid_pipe_connect"];
        //输入口贴图
        var texutre_input = meta.ExpandTextures["fluid_pipe_input"];

        if (cmp != null && cmp is ConnectComponent cc)
        {
            if (cc.AngleState.Up == MathResult.Same)
            {
                context.Node.DrawSetTransformMatrix(GetTransform(pos, 180));
                context.Node.DrawTexture(texutre_connect, Vector2.Zero);
            }

            if (cc.AngleState.Up == MathResult.Input)
            {
                context.Node.DrawSetTransformMatrix(GetTransform(pos, 180));
                context.Node.DrawTexture(texutre_input, Vector2.Zero);
            }

            if (cc.AngleState.Down == MathResult.Same)
            {
                context.Node.DrawSetTransformMatrix(GetTransform(pos, 0));
                context.Node.DrawTexture(texutre_connect, Vector2.Zero);
            }

            if (cc.AngleState.Down == MathResult.Input)
            {
                context.Node.DrawSetTransformMatrix(GetTransform(pos, 0));
                context.Node.DrawTexture(texutre_input, Vector2.Zero);
            }

            if (cc.AngleState.Left == MathResult.Same)
            {
                context.Node.DrawSetTransformMatrix(GetTransform(pos, 90));
                context.Node.DrawTexture(texutre_connect, Vector2.Zero);
            }

            if (cc.AngleState.Left == MathResult.Input)
            {
                context.Node.DrawSetTransformMatrix(GetTransform(pos, 90));
                context.Node.DrawTexture(texutre_input, Vector2.Zero);
            }

            if (cc.AngleState.Right == MathResult.Same)
            {
                context.Node.DrawSetTransformMatrix(GetTransform(pos, 270));
                context.Node.DrawTexture(texutre_connect, Vector2.Zero);
            }

            if (cc.AngleState.Right == MathResult.Input)
            {
                context.Node.DrawSetTransformMatrix(GetTransform(pos, 270));
                context.Node.DrawTexture(texutre_input, Vector2.Zero);
            }

            context.Node.DrawSetTransformMatrix(GetTransform(Vector2I.Zero, 0));
        }
    }
}