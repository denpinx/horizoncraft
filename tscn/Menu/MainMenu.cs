using Godot;
using horizoncraft.script;
using horizoncraft.script.WorldControl;
using System;
using Godot.Collections;
using horizoncraft.script.Config;
using World = HorizonCraft.script;

namespace horizoncraft.script;

public partial class MainMenu : World
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
            Player.Profile.Name = cmd[1];
            GD.Print($"运行参数：{string.Join(",", cmd)}");
        }


        base._Ready();
        player.hotBar.Visible = false;

        ButtonSingle = GetNode<TextureButton>("GuiCanvasLayer/VBoxContainer/Button_Single");
        ButtonHost = GetNode<TextureButton>("GuiCanvasLayer/VBoxContainer/Button_Host");
        ButtonConnect = GetNode<TextureButton>("GuiCanvasLayer/VBoxContainer/Button_Connect");
        TextEdit = GetNode<TextEdit>("GuiCanvasLayer/TextEdit");

        ButtonSingle.Pressed += () =>
        {
            worldMode = WorldMode.Single;
            GetTree().ChangeSceneToFile("res://tscn/world.tscn");
        };
        ButtonHost.Pressed += () =>
        {
            worldMode = WorldMode.MultiplayerHost;
            GetTree().ChangeSceneToFile("res://tscn/world.tscn");
        };
        ButtonConnect.Pressed += () =>
        {
            worldMode = WorldMode.MultiplayerClient;
            GetTree().ChangeSceneToFile("res://tscn/world.tscn");
        };
        TextEdit.Text = Player.Profile.Name;
        TextEdit.TextChanged += () =>
        {
            Player.Profile.Name = TextEdit.Text;
            SaveProfile();
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        player.Visible = false;
        player.Inputable = false;
        base._PhysicsProcess(delta);
        player.Position += Vector2.Left * 1;
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

        if (!FileAccess.FileExists("profile/player.json"))
        {
            Player.Profile = new PlayerProfile()
            {
                Name = "玩家" + new Random().Next()
            };
            SaveProfile();
            return;
        }

        FileAccess fs = FileAccess.Open("profile/player.json", FileAccess.ModeFlags.Read);
        var json_text = fs.GetAsText();
        fs.Close();
        PlayerProfile profile = new();
        profile.ParseDictionary((Dictionary)Json.ParseString(json_text));
        Player.Profile = profile;
        GD.Print($"加载文档:{Player.Profile.Name}");
    }

    private static void SaveProfile()
    {
        if (!DirAccess.DirExistsAbsolute($"profile"))
            DirAccess.MakeDirAbsolute($"profile");
        FileAccess fs = FileAccess.Open("profile/player.json", FileAccess.ModeFlags.Write);
        fs.StoreString(Json.Stringify(Player.Profile.ToDictionary()));
        fs.Close();
    }
}