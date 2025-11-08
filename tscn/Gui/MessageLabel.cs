using Godot;
using Horizoncraft.script.Chat;

/// <summary>
/// 消息标签
/// TODO 待完成
/// </summary>
public partial class MessageLabel : HBoxContainer
{
    public ChatView ParentChatView;
    public MessageData MessageData;
    [Export] MenuBar _menuBar;
    [Export] PopupMenu _popupMenu;
    [Export] Label _label;

    public override void _Ready()
    {
    }

    public void SetMessageData(MessageData data)
    {
        if (data == null)
        {
            GD.PrintErr("MessageData is null");
            return;
        }

        MessageData = data;
        _popupMenu.Title = MessageData.PlayerName;
        _label.Text = MessageData.Message;
    }
}