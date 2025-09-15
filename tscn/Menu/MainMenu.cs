using System;
using Godot;
using Godot.Collections;
using horizoncraft.script;
using horizoncraft.script.Config;
using horizoncraft.script.Expand;
using horizoncraft.script.WorldControl;

namespace HorizonCraft.tscn.Menu;

public partial class MainMenu : horizoncraft.script.World
{
    private CanvasLayer TopCanvasLayer, GuiCanvasLayer;
    private TextureButton ButtonSingle, buttonExit, ButtonConnect;
    private TextEdit TextEdit;

    private PackedScene WorldProfilesScene;

    public override void _Ready()
    {
        WorldProfilesScene = GD.Load<PackedScene>("res://tscn/Menu/WorldListMenu.tscn");

        GuiCanvasLayer = GetNode<CanvasLayer>("GuiCanvasLayer");
        TopCanvasLayer = GetNode<CanvasLayer>("TopCanvasLayer");
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

        ButtonSingle = GetNode<TextureButton>("GuiCanvasLayer/VBoxContainer/HBoxContainer/Button_Single");
        buttonExit = GetNode<TextureButton>("GuiCanvasLayer/VBoxContainer/HBoxContainer2/Button_Exit");
        ButtonConnect = GetNode<TextureButton>("GuiCanvasLayer/VBoxContainer/HBoxContainer/Button_Connect");
        TextEdit = GetNode<TextEdit>("GuiCanvasLayer/TextEdit");

        var worldScene = GD.Load<PackedScene>("res://tscn/world.tscn");
        ButtonSingle.Pressed += () =>
        {
            if (TopCanvasLayer.GetChildCount() == 0)
            {
                GuiCanvasLayer.Visible = false;
                TopCanvasLayer.AddChild(WorldProfilesScene.Instantiate());
            }
        };
        buttonExit.Pressed += () => { QueueFree(); };
        ButtonConnect.Pressed += () =>
        {
            worldMode = WorldMode.MultiplayerClient;
            worldScene.SetName("客户端模式");
            GetTree().ChangeSceneToPacked(worldScene);
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

        if (TopCanvasLayer.GetChildCount() == 0)
        {
            if (!GuiCanvasLayer.Visible) GuiCanvasLayer.Visible = true;
        }
    }


    public override void BlockInterFaceHandle()
    {
        if (PlayerNode?.playerData == null) return;
        if (Service.ChunkService.Chunks.TryGetValue(PlayerNode.playerData.ChunkCoord, out var chunk))
        {
            var coord = (PlayerNode.Position.ToVector2I().MathFloor(16)).Remainder(Chunk.Size);
            var targetpos = new Vector2(PlayerNode.Position.X, chunk.HighMap[coord.X, 0] * 16f);
            PlayerNode.Position = targetpos;
            //var tween = GetTree().CreateTween();
            //tween.TweenProperty(PlayerNode, "position", targetpos, 0.1f);
        }
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