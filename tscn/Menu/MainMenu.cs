using Godot;
using horizoncraft.script;
using horizoncraft.script.WorldControl;
using System;

public partial class MainMenu : World
{
    public override void _Ready()
    {
        _ = Materials.blockmetas;
        PSTilemapLayerChunk = GD.Load<PackedScene>("res://tscn/TileMapLayerChunk.tscn");
        subViewport = GetNode<SubViewport>("CanvasLayer/SubViewport");
        textureRect = GetNode<TextureRect>("CanvasLayer/TextureRect");

        world_name = "preview_world";
        player = GetNode<Player>("Player");
        player.Visible = false;
        timer = GetNode<Timer>("Timer_Tick");
        textureRect.Texture = subViewport.GetTexture();

        timer.Timeout += CilentTick;

        player.world = this;
        player.InputAble = false;
        player.MoreInfo = false;

        chunkManage = new ChunkManageSql(this, ChunkManageSql.WorldMode.Preview);
        chunkManage.LoadHorizon = 2;
        chunkManage.TileMapHorizon = 2;
    chunkManage.OnPlayerMoveChunk();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        player.Position += Vector2.Left * 2;
    }


}
