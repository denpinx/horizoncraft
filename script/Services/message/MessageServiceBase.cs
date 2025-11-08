using System;
using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Chat;

namespace Horizoncraft.script.Services.message;

/// <summary>
/// 消息管理服务
/// 不存储消息日志
/// </summary>
public abstract class MessageServiceBase : ServiceBase
{
    protected PackedScene MessageLabelScene = GD.Load<PackedScene>("res://tscn/Gui/MessageLabel.tscn");

    /// <summary>
    /// 驻留显示的最大消息数
    /// </summary>
    public const int ResidentMessagesCount = 40;

    /// <summary>
    /// 接收新消息时的消息强制显示时间
    /// </summary>
    public const int NewMessageVisibleTick = 40;

    public int VisibleTick = 0;

    protected PlayerNode Player;

    protected List<MessageData> Messages = new();
    protected Dictionary<Guid, MessageLabel> MessageLabels = new();


    public MessageServiceBase(World world) : base(world)
    {
        this.Player = World.PlayerNode;
        world.timer.Timeout += Tick;
    }

    /// <summary>
    /// 时刻更新
    /// </summary>
    protected virtual void Tick()
    {
        UpdateMessages();
        
        if (Player.ChatView.LineEdit.Visible)
        {
            Player.Inputable = false;
        }
        else
        {
            Player.Inputable = true;
        }

        if (VisibleTick <= 0)
        {
            if (!Player.ChatView.LineEdit.Visible)
            {
                Player.ChatView.MessageRoot.Visible = false;
            }
        }
        else
        {
            VisibleTick--;
            Player.ChatView.MessageRoot.Visible = true;
        }
    }

    /// <summary>
    /// 添加信息
    /// </summary>
    /// <param name="message">信息</param>
    /// <returns></returns>
    public virtual bool AddMessage(MessageData message)
    {
        if (message.Message == "") return false;
        if (HasMessage(message.Id))
        {
            GD.PrintErr("Message already exists");
            return false;
        }

        if (Messages.Count > 0 && Messages.Count >= ResidentMessagesCount)
        {
            //删掉第一个
            Messages.RemoveAt(0);
        }

        Messages.Add(message);
        return true;
    }

    /// <summary>
    /// 用户输入的信息
    /// </summary>
    /// <param name="message"></param>
    public virtual void UserInputMessage(MessageData message)
    {
        if (AddMessage(message))
            SyncMessage(message);
    }

    /// <summary>
    /// 更新和同步信息
    /// </summary>
    protected virtual void UpdateMessages()
    {
        foreach (var guid in MessageLabels.Keys)
        {
            if (!HasMessage(guid))
            {
                var ml = MessageLabels[guid];
                if (MessageLabels.Remove(guid))
                    ml.QueueFree();
            }
        }

        foreach (var message in Messages)
        {
            if (!MessageLabels.ContainsKey(message.Id))
            {
                var mls = MessageLabelScene.Instantiate<MessageLabel>();
                mls.Ready += () => mls.SetMessageData(message);
                MessageLabels.Add(message.Id, mls);
                Player.ChatView.MessageRoot.AddChild(mls);
                //公示新信息
                VisibleTick = NewMessageVisibleTick;
            }
        }
    }


    /// <summary>
    /// 同步消息
    /// </summary>
    /// <param name="message">要同步的消息</param>
    protected virtual void SyncMessage(MessageData message)
    {
        //留空
    }

    /// <summary>
    /// 检查消息是否已经存在
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public bool HasMessage(Guid guid)
    {
        return Messages.Find((message) => message.Id == guid) != null;
    }
}