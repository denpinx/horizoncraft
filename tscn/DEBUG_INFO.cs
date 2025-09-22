using Godot;
using System;
using System.Text;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Expand;
using horizoncraft.script.WorldControl;

public partial class DEBUG_INFO : CanvasLayer
{
    Label Label_DEBUG_Right;
    Label Label_DEBUG_Left;
    PlayerNode _playerNode;

    public override void _Ready()
    {
        _playerNode = GetParent<PlayerNode>();
        Label_DEBUG_Right = GetNode<Label>("Control/Label_DEBUG_Right");
        Label_DEBUG_Left = GetNode<Label>("Control/Label_DEBUG_Left");

        _playerNode.GetNode<Timer>("Timer_Tick").Timeout += Update;
    }
    public void Update()
    {
        if (_playerNode?.playerData == null || _playerNode.world == null)
        {
            Label_DEBUG_Left.Visible = false;
            Label_DEBUG_Right.Visible = false;
            return;
        }
        else if (!_playerNode.MoreInfo || !_playerNode.Inputable)
        {
            Label_DEBUG_Left.Visible = false;
            Label_DEBUG_Right.Visible = false;
            return;
        }
        else
        {
            Label_DEBUG_Left.Visible = true;
            Label_DEBUG_Right.Visible = true;
        }

        World world = _playerNode.world;

        Vector2I mouseBlockPos = _playerNode.GetGlobalMousePosition().ToVector2I().MathFloor(16);
        Vector2I mouseChunkPos = mouseBlockPos.MathFloor(Chunk.Size);
        Label_DEBUG_Left.Text = "";
        StringBuilder Text = new StringBuilder();

        //var targetblock = world?.Service?.ChunkService.GetBlock(new Vector3I(mouseBlockPos.X, mouseBlockPos.Y, 1));
        Text.AppendLine($"当前时间：{world?.Service?.GetTimeDay()}天,{world?.Service?.GetTimeHour()}时");
        Text.AppendLine($"全局坐标：{_playerNode.playerData.Coord.X},{_playerNode.playerData.Coord.Y}");

        Text.AppendLine($"区块坐标：{_playerNode.playerData.ChunkCoord.X},{_playerNode.playerData.ChunkCoord.Y}");
        //Text.AppendLine($"TileMap: {world.tileMapLayerChunks.Count}");
        //Text.AppendLine($"显示区块: {world.VisibleChunks.Count}");
        Text.AppendLine($"鼠标位置: {mouseBlockPos.X},{mouseBlockPos.Y} ");
        // if (targetblock != null)
        // {
        //     Text.AppendLine($"-光照: {targetblock.Light} ");
        //     Text.AppendLine($"-状态: {targetblock.State} ");
        //     Text.AppendLine($"-ID: {targetblock.Id} ");
        //     Text.AppendLine($"-Name: {targetblock.BlockMeta.Name} ");
        //
        //     if (targetblock.components.Count > 0)
        //     {
        //         var tick = targetblock.GetComponent<TickComponent>();
        //         if (tick != null) Text.AppendLine($"--Tick值: {tick.Current} / {tick.Max} ");
        //     }
        // }

        //Text.AppendLine($"当前方块坐标: {mouseChunkPos.X},{mouseChunkPos.Y} ");
        Text.AppendLine($"World更新耗时: {world.tick_use_time}MS");
        Text.AppendLine($"时间: {world.Service.TickTimes}");

        foreach (Func<string> func in PlayerNode.GetInformation.Values)
            Text.AppendLine(func());

        Label_DEBUG_Left.Text = Text.ToString();
        StringBuilder right = new StringBuilder();
        foreach (var sets in world.Service.PlayerService.Players)
        {
            var player = sets.Value;
            right.AppendLine(
                $"在线玩家[{sets.Key}] 坐标:[{player.ChunkCoord.X},{player.ChunkCoord.Y}],id{player.PeerId}");
        }


        Label_DEBUG_Right.Text = right.ToString();
    }
}