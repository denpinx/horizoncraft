using System;
using System.Text.Json;
using Godot;
using Godot.Collections;
using Horizoncraft.script;
using Horizoncraft.script.Config;
using Horizoncraft.script.Expand;
using Horizoncraft.script.I18N;
using HorizonCraft.script.Services.world;
using Horizoncraft.script.Utility;
using Horizoncraft.script.WorldControl;

namespace HorizonCraft.tscn.Menu;

public partial class MainMenu : World, ITranslatable
{
    [Export] private CanvasLayer TopCanvasLayer, GuiCanvasLayer;
    [Export] private Button ButtonSingle, buttonExit, ButtonConnect;
    [Export] private TextEdit TextEdit;

    private PackedScene WorldProfilesScene = GD.Load<PackedScene>("res://tscn/Menu/WorldListMenu.tscn");
    private PackedScene MultiplayerConnectScene = GD.Load<PackedScene>("res://tscn/Menu/MultiplayerConnect.tscn");

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
        ButtonSingle.Pressed += () =>
        {
            if (TopCanvasLayer.GetChildCount() == 0)
            {
                GuiCanvasLayer.Visible = false;
                var node = WorldProfilesScene.Instantiate<WorldListMenu>();
                node.OnChangeScene += QueueFree;
                TopCanvasLayer.AddChild(node);
            }
        };
        buttonExit.Pressed += () => { GetTree().Quit(); };
        ButtonConnect.Pressed += () =>
        {
            if (TopCanvasLayer.GetChildCount() == 0)
            {
                GuiCanvasLayer.Visible = false;
                var node = MultiplayerConnectScene.Instantiate<MultiplayerConnect>();
                node.OnChangeScene += QueueFree;
                TopCanvasLayer.AddChild(node);
            }
        };
        TextEdit.Text = PlayerNode.Profile.Name;
        TextEdit.TextChanged += () =>
        {
            var old = PlayerNode.Profile.Name;
            PlayerNode.Profile.Name = TextEdit.Text;
            Service.PlayerService.ChangeName(old, PlayerNode.Profile.Name);
            SaveProfile();
        };

        PlayerNode.Visible = false;
        PlayerNode.BaseInputable = false;
        LanguageManage.SetTargetLang("cn", GetTree());
        SetRunConfig();
    }

    public override void _Process(double delta)
    {
        if (RunConfig.Mode == RunMode.Server)
        {
            var profile = DirUtility.GetWorldProfile(HorizonCraft.RunConfig.WorldName);
            if (profile != null)
            {
                World.worldMode = World.WorldMode.MultiplayerHost;
                World.WorldName = profile.WorldName;
                World.Seed = profile.WorldSeed;
                HostWorldService.Port = HorizonCraft.RunConfig.Port;
                GetTree().ChangeSceneToFile("res://tscn/world.tscn");

                GD.Print("创建服务端");
            }
            else
            {
                World.worldMode = World.WorldMode.MultiplayerHost;
                World.WorldName = HorizonCraft.RunConfig.WorldName;
                World.Seed = HorizonCraft.RunConfig.WorldSeed;
                HostWorldService.Port = HorizonCraft.RunConfig.Port;

                GetTree().ChangeSceneToFile("res://tscn/world.tscn");
                GD.Print("创建新的存档");
            }

            QueueFree();
        }

        base._Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
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
        }
    }

    public override void _ExitTree()
    {
        SaveProfile();
        this.Service = null;
        GD.Print("Exit Tree");
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


    private void SetRunConfig()
    {
        if (OS.HasFeature("dedicated_server"))
        {
            RunConfig.Mode = RunMode.Server;
            if (FileAccess.FileExists("run.json"))
            {
                FileAccess fileAccess = FileAccess.Open("run.json", FileAccess.ModeFlags.Read);
                string jsonText = fileAccess.GetAsText();
                fileAccess.Close();
                var dict = JsonCleaner.FromJson(jsonText);

                PlayerNode.Profile.Name = Guid.NewGuid().ToString();
                if (dict.TryGetValue("world-name", out var name))
                {
                    RunConfig.WorldName = (string)name;
                }

                if (dict.TryGetValue("world-seed", out var seed))
                {
                    RunConfig.WorldSeed = long.Parse((string)seed);
                }

                if (dict.TryGetValue("ip", out var ip))
                {
                    RunConfig.Ip = (string)ip;
                }

                if (dict.TryGetValue("port", out var port))
                {
                    RunConfig.Port = (int)port;
                }
            }
            else
            {
                Console.Write("世界名:");
                var worldName = Console.ReadLine();
                Console.Write("种子:");
                var worldSeed = Console.ReadLine();
                Console.Write("端口:");
                var port = Console.ReadLine();
                RunConfig.WorldName = worldName;
                RunConfig.Port = int.Parse(port);
                if (long.TryParse(worldSeed, out var result))
                {
                    RunConfig.WorldSeed = result;
                }

                var file = FileAccess.Open("run.json", FileAccess.ModeFlags.Write);
                file.StoreString(Json.Stringify(RunConfig.ToDictionary()));
                file.Close();
            }
        }
    }

    public void TranslateChange()
    {
        buttonExit.Text = "Exit".Trprefix("ui");
        ButtonSingle.Text = "Single Play".Trprefix("ui");
        ButtonConnect.Text = "Connect Server".Trprefix("ui");
    }
}