using Godot;
using System;
using System.Text;
using Horizoncraft.script;
using Horizoncraft.script.Expand;
using Horizoncraft.script.WorldControl;

public partial class DEBUG_INFO : CanvasLayer
{
    [Export] Label Label_DEBUG_Right;
    [Export] Label Label_DEBUG_Left;
    [Export] PlayerNode _playerNode;
    [Export] Timer _timer;

    public override void _Ready()
    {
        _playerNode = GetParent<PlayerNode>();
        Label_DEBUG_Right = GetNode<Label>("Control/Label_DEBUG_Right");
        Label_DEBUG_Left = GetNode<Label>("Control/Label_DEBUG_Left");

        _timer.Timeout += Update;
    }

    public void Update()
    {
        if (_playerNode?.playerData == null || _playerNode.world == null)
        {
            Label_DEBUG_Left.Visible = false;
            Label_DEBUG_Right.Visible = false;
            return;
        }
        else if (!_playerNode.MoreInfo || !_playerNode.BaseInputable)
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

        Text.AppendLine($"当前时间：{world?.Service.GetTime()}");
        Text.AppendLine($"全局坐标：{_playerNode.playerData.Coord.X},{_playerNode.playerData.Coord.Y}");
        Text.AppendLine($"区块坐标：{_playerNode.playerData.ChunkCoord.X},{_playerNode.playerData.ChunkCoord.Y}");
        Text.AppendLine($"鼠标位置: {mouseBlockPos.X},{mouseBlockPos.Y} ");
        Text.AppendLine($"世界更新总耗时: {world.TimeConsumingμs}μs");
        Text.AppendLine($"时间: {world.Service.TickTimes}");

        foreach (Func<string> func in PlayerNode.GetInformation.Values)
            Text.AppendLine(func());

        Label_DEBUG_Left.Text = Text.ToString();
        StringBuilder right = new StringBuilder();
        foreach (var sets in world.Service.PlayerService.Players)
        {
            var player = sets.Value;
            right.AppendLine(
                $"在线玩家[{sets.Key}]状态:{player.State}, 坐标:[{player.ChunkCoord.X},{player.ChunkCoord.Y}],id{player.PeerId}");
        }


        Label_DEBUG_Right.Text = right.ToString();
    }
}