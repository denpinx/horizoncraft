using Godot;
using horizoncraft.script.WorldControl;

public partial class DebugView : Node2D
{
    public Chunk chunk;
    Font font = new SystemFont();
    public static bool DEBUG = false;
    public override void _Draw()
    {
        if (!DEBUG) return;

        if (chunk == null)
        {
            DrawRect(new Rect2(0, 0, new(16 * Chunk.Size, 16 * Chunk.Size)), Color.Color8(192, 192, 129));
            return;
        }
        Color color;
        if (chunk.coord.X % 2 == 0)
        {
            if (chunk.coord.Y % 2 == 0)
                color = Color.Color8(255, 128, 128);
            else
                color = Color.Color8(128, 255, 128);
        }
        else
        {
            if (chunk.coord.Y % 2 == 0)
                color = Color.Color8(255, 128, 255);
            else
                color = Color.Color8(255, 255, 128);
        }

        DrawPolyline(new Vector2[]{
            new Vector2(0,0),
            new Vector2(0,16*Chunk.Size),
            new Vector2(16*Chunk.Size,16*Chunk.Size),
            new Vector2(16*Chunk.Size,0),
            new Vector2(0,0)
        }, color,4
        );
        
        //DrawRect(new Rect2(0, 0, new(16 * Chunk.Size, 16 * Chunk.Size)), color);
        DrawString(font, new(0, 9 * Chunk.Size), $"坐标：{chunk.coord.X}，{chunk.coord.Y} ");
        DrawString(font, new(0, 10 * Chunk.Size), $"生成耗时：{chunk.SpawnCostTime} ms");
        DrawString(font, new(0, 14 * Chunk.Size), $"生物群系类型：{chunk.BiomeType} ");
    }
}
