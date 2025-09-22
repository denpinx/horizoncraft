using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using horizoncraft.script;
using horizoncraft.script.Net;
using horizoncraft.script.WorldControl.Tool;
using FileAccess = Godot.FileAccess;

public partial class WorldListMenu : Control
{
    public WorldProfile SelectedWorld;
    private VBoxContainer ListRoot;
    private VBoxContainer Root;
    private List<WorldProfile> worldList = new List<WorldProfile>();
    private List<WorldInfoLabel> worldInfoLabels = new List<WorldInfoLabel>();
    private TextureButton buttonBackMainMenu, buttonLoadWorld, buttonMultiplayer, buttonCreateWorld, buttonRemove;
    private Label buttonRemoveLabel;
    private PackedScene WorldCreateScene;
    private bool ConfirmDel = false;

    public override void _Ready()
    {
        WorldCreateScene = GD.Load<PackedScene>("res://tscn/Menu/create_world_config.tscn");
        var WorldProfilesScene = GD.Load<PackedScene>("res://tscn/Menu/WorldListMenu.tscn");
        ListRoot = (VBoxContainer)GetNode(
            "VBoxContainer/HBoxContainer/PanelContainer3/VBoxContainer/HBoxContainer2/ScrollContainer/VBoxContainer");
        Root = GetNode<VBoxContainer>("VBoxContainer");
        buttonLoadWorld =
            GetNode<TextureButton>("VBoxContainer/HBoxContainer3/PanelContainer/HBoxContainer2/Button_LoadGame");
        buttonBackMainMenu =
            GetNode<TextureButton>("VBoxContainer/HBoxContainer3/PanelContainer/HBoxContainer2/Button_BackMainMenu");
        buttonCreateWorld =
            GetNode<TextureButton>("VBoxContainer/HBoxContainer3/PanelContainer/HBoxContainer2/Button_CreateWorld");
        buttonMultiplayer =
            GetNode<TextureButton>("VBoxContainer/HBoxContainer3/PanelContainer/HBoxContainer2/Button_Multiplayer");
        buttonRemove =
            GetNode<TextureButton>("VBoxContainer/HBoxContainer3/PanelContainer/HBoxContainer2/Button_Remove");
        buttonRemoveLabel =
            GetNode<Label>("VBoxContainer/HBoxContainer3/PanelContainer/HBoxContainer2/Button_Remove/Label");
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
            if (!ConfirmDel)
            {
                ConfirmDel = true;
                buttonRemoveLabel.Text = $"确定删除世界 {SelectedWorld.WorldName}";
                return;
            }

            ConfirmDel = false;

            //先用手动删除代替
            if (Directory.Exists("save"))
            {
                OS.ShellOpen("save");
            }


            // TODO 有问题，删不掉
            // string path = $"save/{SelectedWorld.WorldName}";
            // if (Directory.Exists(path))
            // {
            //     Directory.Delete(path, true);
            //     foreach (var wil in worldInfoLabels)
            //     {
            //         wil.QueueFree();
            //     }
            //
            //     // worldInfoLabels.Clear();
            //     // worldList = GetWorldFiles();
            //     // UpdateWorldList();
            //     GD.Print($"删除世界{path}");
            // }
            // SelectedWorld = null;
            // ConfirmDel = false;
            // buttonRemoveLabel.Text = $"删除世界";
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
                GD.Print("世界名" + dir);

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
}