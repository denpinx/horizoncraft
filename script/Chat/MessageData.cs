using System;
using MemoryPack;

namespace horizoncraft.script.Chat;

/// <summary>
/// 消息数据
/// </summary>
[MemoryPackable]
public partial class MessageData
{
    public Guid Id;
    public string PlayerName;
    public string Message;
}