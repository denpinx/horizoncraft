using Godot;
using System;
using horizoncraft.script;
using HorizonCraft.script.Services.world;

public partial class LoadingMenu : CanvasLayer
{
    public PlayerNode playerNode;
    private AnimationPlayer animationPlayer;
    private Label label;
    private bool quit = false;
    private double loadingTimer = 0;

    public override void _Ready()
    {
        Visible = true;
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        label = GetNode<Label>("TextureRect/VBoxContainer/Label");
        animationPlayer.Play("loading");
        animationPlayer.AnimationFinished += (name =>
        {
            if (name == "loadover")
            {
                QueueFree();
            }
        });
    }

    public override void _Process(double delta)
    {
        loadingTimer += delta;

        if (playerNode != null && playerNode.playerData != null && !quit)
        {
            quit = true;
            animationPlayer.Play("loadover");
        }
        else if (playerNode != null && playerNode.world.Service is ClientWorldService cws)
        {
            switch (loadingTimer)
            {
                case < 10 and > 2:
                {
                    label.Text = "等待服务器响应中...";
                    break;
                }
                case > 10:
                {
                    label.Text = "服务器长时间未响应...";
                    break;
                }
            }
        }
        else
        {
            switch (loadingTimer)
            {
                case < 5 and > 2:
                {
                    label.Text = "缓慢加载中...";
                    break;
                }
                case > 5:
                {
                    label.Text = "龟速加载中...";
                    break;
                }
            }
        }
    }
}