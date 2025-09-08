using System;
using Godot;
using Godot.Collections;
using horizoncraft.script;
using horizoncraft.script.Config;

namespace HorizonCraft.tscn.Menu;

public partial class MainMenu : horizoncraft.script.World
{
    private TextureButton ButtonSingle, ButtonHost, ButtonConnect;
    private TextEdit TextEdit;

    public override void _Ready()
    {
        worldMode = WorldMode.Preview;
        LoadProfile();

        var cmd = OS.GetCmdlineArgs();
        if (cmd.Length > 1)
        {
            PlayerNode.Profile.Name = cmd[1];
            GD.Print($"运行参数：{string.Join(",", cmd)}");
        }


        base._Ready();
        PlayerNode.hotBar.Visible = false;

        ButtonSingle = GetNode<TextureButton>("GuiCanvasLayer/VBoxContainer/Button_Single");
        ButtonHost = GetNode<TextureButton>("GuiCanvasLayer/VBoxContainer/Button_Host");
        ButtonConnect = GetNode<TextureButton>("GuiCanvasLayer/VBoxContainer/Button_Connect");
        TextEdit = GetNode<TextEdit>("GuiCanvasLayer/TextEdit");

        var worldScene = GD.Load<PackedScene>("res://tscn/world.tscn");
        ButtonSingle.Pressed += () =>
        {
            worldMode = WorldMode.Single;
            worldScene.SetName("单机模式");
            GetTree().ChangeSceneToPacked(worldScene);
            //GetTree().ChangeSceneToFile("res://tscn/world.tscn");
        };
        ButtonHost.Pressed += () =>
        {
            worldMode = WorldMode.MultiplayerHost;
            worldScene.SetName("主机模式");
            GetTree().ChangeSceneToPacked(worldScene);
            //GetTree().ChangeSceneToFile("res://tscn/world.tscn");
        };
        ButtonConnect.Pressed += () =>
        {
            worldMode = WorldMode.MultiplayerClient;
            worldScene.SetName("客户端模式");
            GetTree().ChangeSceneToPacked(worldScene);
            //GetTree().ChangeSceneToFile("res://tscn/world.tscn");
        };
        TextEdit.Text = PlayerNode.Profile.Name;
        TextEdit.TextChanged += () =>
        {
            PlayerNode.Profile.Name = TextEdit.Text;
            SaveProfile();
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        PlayerNode.Visible = false;
        PlayerNode.Inputable = false;
        base._PhysicsProcess(delta);
        PlayerNode.Position += Vector2.Left * 1;
    }

    public override void _ExitTree()
    {
        SaveProfile();
    }

    private static void LoadProfile()
    {
        if (!DirAccess.DirExistsAbsolute($"profile"))
        {
            DirAccess.MakeDirAbsolute($"profile");
        }

        if (!FileAccess.FileExists("profile/local.json"))
        {
            PlayerNode.Profile = new LocalProfile()
            {
                Name = "玩家" + new Random().Next()
            };
            SaveProfile();
            return;
        }

        FileAccess fs = FileAccess.Open("profile/local.json", FileAccess.ModeFlags.Read);
        var json_text = fs.GetAsText();
        fs.Close();
        LocalProfile profile = new();
        profile.ParseDictionary((Dictionary)Json.ParseString(json_text));
        PlayerNode.Profile = profile;
        GD.Print($"加载文档:{PlayerNode.Profile.Name}");
    }

    private static void SaveProfile()
    {
        if (!DirAccess.DirExistsAbsolute($"profile"))
            DirAccess.MakeDirAbsolute($"profile");
        FileAccess fs = FileAccess.Open("profile/local.json", FileAccess.ModeFlags.Write);
        fs.StoreString(Json.Stringify(PlayerNode.Profile.ToDictionary()));
        fs.Close();
    }
}