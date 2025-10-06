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
    PackedScene MessageLabelScene = GD.Load<PackedScene>("res://tscn/Gui/MessageLabel.tscn");
    [Export] public PlayerNode Player;
    [Export] public LineEdit LineEdit;
    [Export] public VBoxContainer MessageRoot;
    double openFrozen = 0.5f;

    public override void _Ready()
    {
        LineEdit.GrabFocus();
        LineEdit.TextSubmitted += (text) =>
        {
            var guid = Guid.NewGuid();
            Player.world.Service.MessageService.UserInputMessage(new MessageData()
            {
                Id = guid,
                PlayerName = Player.playerData.Name,
                Message = LineEdit.Text,
            });
            LineEdit.Text = "";
            LineEdit.Visible = false;
            openFrozen = 0.25f;
        };
    }

    public override void _Process(double delta)
    {
        if (openFrozen > 0) openFrozen -= delta;
        else openFrozen = 0;
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustReleased("chat"))
        {
            if (openFrozen != 0) return;
            if (LineEdit.HasFocus()) return;

            if (!LineEdit.Visible)
            {
                MessageRoot.Visible = true;
                LineEdit.Visible = true;
                LineEdit.GrabFocus();
            }
        }

        if (Input.IsActionJustReleased("OpenOperatingMenu"))
        {
            if (LineEdit.Visible)
            {
                LineEdit.Visible = false;
            }
        }
    }
}