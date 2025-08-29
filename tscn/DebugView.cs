using Godot;
using horizoncraft.script.WorldControl;

public partial class DebugView : Node2D
{
    public Chunk chunk;
    Font font = new SystemFont();
    public static bool DEBUG = false;
    public long time = 0;

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

        DrawPolyline(new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(0, 16 * Chunk.Size),
                new Vector2(16 * Chunk.Size, 16 * Chunk.Size),
                new Vector2(16 * Chunk.Size, 0),
                new Vector2(0, 0)
            }, color, 4
        );
        var biomebase = BiomeManage.GetBiomeAsName(chunk.BiomeType);
        DrawRect(new Rect2(0, 0, new(16 * Chunk.Size, 16 * Chunk.Size)), biomebase.color);
        DrawString(font, new(0, 1 * Chunk.Size), $"坐标：{chunk.coord.X}，{chunk.coord.Y} ");
        DrawString(font, new(0, 2 * Chunk.Size), $"生成耗时：{chunk.SpawnCostTime} ms");
        DrawString(font, new(0, 3 * Chunk.Size), $"生物群系类型：{chunk.BiomeType} ");
        DrawString(font, new(0, 4 * Chunk.Size), $"更新时间戳：{chunk.version} ");
        DrawString(font, new(0, 5 * Chunk.Size), $"Tick数：{chunk.TickList.Count} ");
        DrawString(font, new(0, 6 * Chunk.Size), $"光源对象：{chunk.LightList.Count} ");
        DrawString(font, new(0, 8 * Chunk.Size), $"Tilemap标识：{time} ");
    }
}