using Godot;
using System;
using Horizoncraft.script;
using Horizoncraft.script.RenderSystem;
using Horizoncraft.script.WorldControl;

public partial class RenderNode : Node2D
{
    /// <summary>
    /// 当前节点的在世界的Z轴
    /// </summary>
    [Export] public int Z = 0;

    public Chunk Chunk { get; set; }
    public World World { get; set; }

    public override void _Ready()
    {
    }

    public void SetConfig(Chunk chunk, World world)
    {
        this.Chunk = chunk;
        this.World = world;
    }

    public override void _Draw()
    {
        if (Chunk == null) return;
        for (int x = 0; x < Chunk.Size; x++)
        {
            for (int y = 0; y < Chunk.Size; y++)
            {
                BlockData block = Chunk.GetBlock(x, y, Z);

                if (Z == 0)
                {
                    if (Chunk.GetBlock(x, y, 1).BlockMeta.Cube)
                        continue;
                }
                else if (block.Light == 0)
                    continue;

                RenderContext context = new()
                {
                    Service = World.Service,
                    Chunk = Chunk,
                    Node = this,
                    Block = block,
                    Position = new Vector3I(x, y,Z),
                    GlobalPosition = new Vector3I(Chunk.X * Chunk.Size + x, Chunk.Y * Chunk.Size + y,Z)
                };
                foreach (var index in block.BlockMeta.RenderSystem)
                {
                    var render = RenderSystemManager.GetRender(index);
                    render?.OnDraw(context);
                }
            }
        }
    }
}