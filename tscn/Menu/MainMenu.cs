using System;
using Godot;
using Godot.Collections;
using Horizoncraft.script;
using Horizoncraft.script.Config;
using Horizoncraft.script.Expand;
using Horizoncraft.script.I18N;
using Horizoncraft.script.WorldControl;

namespace HorizonCraft.tscn.Menu;

public partial class MainMenu : Horizoncraft.script.World, ITranslatable
{
    [Export] private CanvasLayer TopCanvasLayer, GuiCanvasLayer;
    [Export] private Button ButtonSingle, buttonExit, ButtonConnect;
    [Export] private TextEdit TextEdit;

    private PackedScene WorldProfilesScene = GD.Load<PackedScene>("res://tscn/Menu/WorldListMenu.tscn");
    private PackedScene WorldScene = GD.Load<PackedScene>("res://tscn/world.tscn");

    public override void _Ready()
    {
        worldMode = WorldMode.Preview;
        LoadProfile();

        var cmd = OS.GetCmdlineArgs();
        if (cmd.Length > 2)
        {
            PlayerNode.Profile.Name = cmd[2];
            GD.Print($"运行参数：{string.Join(",", cmd)}");
        }


        base._Ready();
        PlayerNode.hotBar.Visible = false;

        ButtonSingle.Pressed += () =>
        {
            if (TopCanvasLayer.GetChildCount() == 0)
            {
                GuiCanvasLayer.Visible = false;
                var node = WorldProfilesScene.Instantiate<WorldListMenu>();
                node.OnChangeScene += () =>
                {
                    this.Service = null;
                    QueueFree();
                };
                TopCanvasLayer.AddChild(node);
            }
        };
        buttonExit.Pressed += () => { GetTree().Quit(); };
        ButtonConnect.Pressed += () =>
        {
            worldMode = WorldMode.MultiplayerClient;
            GetTree().ChangeSceneToPacked(WorldScene);
        };
        TextEdit.Text = PlayerNode.Profile.Name;
        TextEdit.TextChanged += () =>
        {
            PlayerNode.Profile.Name = TextEdit.Text;
            SaveProfile();
        };


        LanguageManage.SetTargetLang("cn", GetTree());
    }

    public override void _PhysicsProcess(double delta)
    {
        PlayerNode.Visible = false;
        PlayerNode.BaseInputable = false;
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

    public void TranslateChange()
    {
        buttonExit.Text = "Exit".Trprefix("ui");
        ButtonSingle.Text = "Single Play".Trprefix("ui");
        ButtonConnect.Text = "Connect Server".Trprefix("ui");
    }
}