using Godot;
using System;
using System.Collections.Generic;
using horizoncraft.script;
using horizoncraft.script.Chat;

/// <summary>
/// 聊天视图
/// TODO 待完成
/// </summary>
public partial class ChatView : Control
{
    public Dictionary<Guid, MessageData> Messages = new();
    public Dictionary<Guid, MessageLabel> MessageLabels = new();
    LineEdit LineEdit;
    private VBoxContainer MessageRoot;
    PackedScene MessageLabelScene;

    public override void _Ready()
    {
        MessageLabelScene = GD.Load<PackedScene>("res://tscn/Gui/MessageLabel.tscn");
        LineEdit = GetNode<LineEdit>("Box/LineEdit");
        MessageRoot = GetNode<VBoxContainer>("Box/InstantMessag");
        LineEdit.GrabFocus();
        LineEdit.TextSubmitted += (text) =>
        {
            var guid = Guid.NewGuid();
            Messages.Add(guid, new MessageData()
            {
                Player = new PlayerData()
                {
                    Name = "test",
                },
                Id = guid,
                Message = LineEdit.Text
            });
            LineEdit.Text = "";
        };
    }

    public override void _Process(double delta)
    {
        foreach (var msg in Messages.Values)
        {
            if (MessageLabels.ContainsKey(msg.Id)) continue;

            var ml = MessageLabelScene.Instantiate<MessageLabel>();
            ml.ParentChatView = this;
            ml.MessageData = msg;
            MessageLabels.Add(msg.Id,ml);
            MessageRoot.AddChild(ml);
        }

        foreach (var guid in MessageLabels.Keys)
        {
            var label = MessageLabels[guid];
            if (Messages.ContainsKey(guid)) continue;
            
            label.QueueFree();
            MessageLabels.Remove(guid);
        }
    }
}