using System;

namespace horizoncraft.script.Chat;

/// <summary>
/// 消息数据
/// </summary>
public class MessageData
{
    public Guid Id;
    public PlayerData Player;
    public string Message;
}