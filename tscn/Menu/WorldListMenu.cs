using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using horizoncraft.script;
using horizoncraft.script.I18N;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl.Tool;
using FileAccess = Godot.FileAccess;

public partial class WorldListMenu : Control, ITranslatable
{
    public WorldProfile SelectedWorld;
    private PackedScene WorldCreateScene = GD.Load<PackedScene>("res://tscn/Menu/create_world_config.tscn");

    [Export] private Button buttonBackMainMenu, buttonLoadWorld, buttonMultiplayer, buttonCreateWorld, buttonRemove;
    [Export] private VBoxContainer ListRoot;
    [Export] private VBoxContainer Root;
    private List<WorldInfoLabel> worldInfoLabels = new List<WorldInfoLabel>();
    private List<WorldProfile> worldList = new List<WorldProfile>();

    public override void _Ready()
    {
        worldList = GetWorldFiles();
        UpdateWorldList();

        buttonBackMainMenu.Pressed += () => { QueueFree(); };
        buttonCreateWorld.Pressed += () => { AddChild(WorldCreateScene.Instantiate()); };
        buttonLoadWorld.Pressed += () =>
        {
            World.worldMode = World.WorldMode.Single;
            World.WorldName = SelectedWorld.WorldName;
            World.Seed = SelectedWorld.WorldSeed;
            GetTree().ChangeSceneToFile("res://tscn/world.tscn");
            QueueFree();
        };
        buttonMultiplayer.Pressed += () =>
        {
            World.worldMode = World.WorldMode.MultiplayerHost;
            World.WorldName = SelectedWorld.WorldName;
            World.Seed = SelectedWorld.WorldSeed;
            GetTree().ChangeSceneToFile("res://tscn/world.tscn");
            QueueFree();
        };
        buttonRemove.Pressed += () =>
        {
            if (SelectedWorld == null) return;
            //先用手动删除代替
            if (Directory.Exists("save"))
            {
                OS.ShellOpen("save");
            }
        };
    }

    public override void _Process(double delta)
    {
        buttonLoadWorld.Visible = (SelectedWorld != null);
        buttonMultiplayer.Visible = (SelectedWorld != null);
        buttonRemove.Visible = (SelectedWorld != null);
        Root.Visible = GetChildCount() == 1;
    }

    /// <summary>
    /// 更新列表
    /// </summary>
    private void UpdateWorldList()
    {
        var wil = GD.Load<PackedScene>("res://tscn/world_info_label.tscn");
        foreach (var profile in worldList)
        {
            var node = wil.Instantiate<WorldInfoLabel>();
            node.Parent = this;
            ListRoot.AddChild(node);
            node.SetWorldProfile(profile);
            worldInfoLabels.Add(node);
        }
    }

    /// <summary>
    /// 获取存档文件内的所有存档
    /// </summary>
    /// <returns></returns>
    private List<WorldProfile> GetWorldFiles()
    {
        List<WorldProfile> worldFiles = new List<WorldProfile>();
        if (!DirAccess.DirExistsAbsolute("save")) return worldFiles;
        DirAccess savedir = DirAccess.Open("save");
        var dirs = savedir.GetDirectories();

        foreach (string dir in dirs)
        {
            if (FileAccess.FileExists($"save/{dir}/data.db"))
            {
                GD.Print($"识别到存档：{dir}");

                using (var conn = SqliteTool.InitSqlite(dir))
                {
                    var file = conn.GetWorldProfileByteData("WorldProfile");
                    if (file != null)
                    {
                        worldFiles.Add(file);
                    }

                    //conn.SafeCloseWAL();
                }
            }
        }

        return worldFiles;
    }

    public void TranslateChange()
    {
        buttonBackMainMenu.Text = "ui.Back Main Menu".Trprefix();
        buttonLoadWorld.Text = "ui.Load World".Trprefix();
        buttonCreateWorld.Text = "ui.Create New World".Trprefix();
        buttonMultiplayer.Text = "ui.Create Multiplayer".Trprefix();
        buttonRemove.Text = "ui.Back Main Menu".Trprefix();
    }
}