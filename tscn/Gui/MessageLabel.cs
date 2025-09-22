using Godot;
using System;
using horizoncraft.script.Chat;

/// <summary>
/// 消息标签
/// TODO 待完成
/// </summary>
public partial class MessageLabel : HBoxContainer
{
    public ChatView ParentChatView;
    public MessageData MessageData;
    MenuBar _menuBar;
    PopupMenu _popupMenu;
    Label _label;

    public override void _Ready()
    {
        _menuBar = GetNode<MenuBar>("MenuBar");
        _popupMenu = GetNode<PopupMenu>("MenuBar/PopupMenu");
        _label = GetNode<Label>("Message");
    }
    
    public override void _Process(double delta)
    {
        if (MessageData != null)
        {
            _popupMenu.Title = MessageData.Player.Name;
            _label.Text = MessageData.Message;
        }
    }
}