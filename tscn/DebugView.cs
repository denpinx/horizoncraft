using System;
using System.Collections.Generic;
using Godot;
using Horizoncraft.script.WorldControl;

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
            DrawRect(new Rect2(0, 0, new(16 * Chunk.Size, 16 * Chunk.Size)), Color.Color8(192,0, 0));
            return;
        }
        DrawString(font, new(0, 11 * Chunk.Size), $"time:{DateTime.Now}");
        DrawString(font, new(0, 12 * Chunk.Size), $"update_tilemap:{chunk.update_tilemap}");
        DrawString(font, new(0, 13 * Chunk.Size), $"update_server:{chunk.update_server}");
        
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
        if (biomebase == null)
        {
            GD.Print("biomebase == null");
            return;
        }
        DrawRect(new Rect2(0, 0, new(16 * Chunk.Size, 16 * Chunk.Size)), biomebase.DebugColor);
        List<string> list = new()
        {
            $"time:{DateTime.Now}",
            $"区块生成耗时：{chunk.SpawnCostTime_μs} μs",
            $"坐标：{chunk.coord.X}，{chunk.coord.Y} ",
            $"生物群系类型：{chunk.BiomeType} ",
            $"更新时间戳：{chunk.version} ",
            $"光源对象：{chunk.LightList.Count}个 ",
            $"Tick对象：{chunk.TickList.Count} 个",
            $"Tick：{chunk.TickUsedTime_μs} μs/t ",
            $"LightUpdateTime：{chunk.LightUpdateTime} tick ",
        };

        int index = 1;
        foreach (var str in list)
            DrawString(font, new(0, index++ * Chunk.Size), str);
    }
}