using Godot;
using horizoncraft.script.WorldControl;

public partial class DebugView : Node2D
{
    public Chunk chunk;
    Font font = new SystemFont();
    public static bool DEBUG = true;
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

        DrawRect(new Rect2(0, 0, new(16 * Chunk.Size, 16 * Chunk.Size)), color);
        DrawString(font, new(0, 8 * Chunk.Size), $"地形是否生成：{chunk.spawn} ");
        DrawString(font, new(0, 9 * Chunk.Size), $"坐标：{chunk.coord.X}，{chunk.coord.Y} ");
        DrawString(font, new(0, 10 * Chunk.Size), $"生成耗时：{chunk.SpawnCostTime} ");
        DrawString(font, new(0, 11 * Chunk.Size), $"加载耗时：{chunk.LoadCostTime} ");
        DrawString(font, new(0, 12 * Chunk.Size), $"生成次数：{chunk.spawncount} ");
        DrawString(font, new(0, 14 * Chunk.Size), $"生物群系类型：{chunk.BiomeType} ");
    }
}
