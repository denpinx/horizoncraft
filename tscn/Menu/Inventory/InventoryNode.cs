using Godot;
using System.Collections.Generic;
using horizoncraft.script;
using horizoncraft.script.Components;
using horizoncraft.script.Events.player;
using horizoncraft.script.Inventory;
using horizoncraft.script.WorldControl;

/// <summary>
/// 通用菜单节点,根据节点自定义
/// </summary>
public partial class InventoryNode : CanvasLayer
{
    [Export] public int TargetNodeCount = 0;
    [Export] public int PlayerNodeCount = 36;
    [Export] public Control TargetInventorySlotRoot;
    [Export] public string TargetInventorySlotName = "TargetInvSlot";
    [Export] public Control PlayerInventorySlotRoot;
    [Export] public string PlayerInventorySlotName = "PlayerInvSlot";

    public string TargetNodePath = "MarginContainer/VBoxContainer/TargetInvBase/TargetInvSlot";
    public string PlayerNodePath = "MarginContainer/GridContainer/PlayerInvSlot";
    public List<string> TargetNodePaths = new();

    public PlayerNode PlayerNode;

    //public Vector3I TargetBlockGlobalPos = new Vector3I();

    public BlockData TargetBlock = null;

    public List<InvSlot> PlayerSlots = new();
    public List<InvSlot> TargetSlots = new();
    public Sprite2D HandItem;

    //private CanvasLayer playerNode;

    public override void _Ready()
    {
        SetLayer(10);

        Timer timer = new Timer();
        timer.Autostart = true;
        timer.SetWaitTime(0.05);
        timer.Timeout += UpdateGui;
        AddChild(timer);
        Init();
    }

    public void Init()
    {
        HandItem = new Sprite2D();
        AddChild(HandItem);
        // if (HasNode("ItemManage"))
        // {
        //     GetNode<ItemManage>("ItemManage");
        // }


        if (TargetNodePaths.Count > 0)
        {
            int i = 0;
            foreach (var path in TargetNodePaths)
            {
                InvSlot s = GetNode<InvSlot>(path);
                s.index = i++;
                s.LeftClick += OnPlayerLeftClickItem;
                s.RightClick += OnPlayerRightClickItem;
                TargetSlots.Add(s);
            }
        }
        else
        {
            for (int i = 0; i < TargetNodeCount; i++)
            {
                InvSlot s = TargetInventorySlotRoot.GetNode<InvSlot>(TargetInventorySlotName + i);

                s.index = i;
                s.LeftClick += OnPlayerLeftClickItem;
                s.RightClick += OnPlayerRightClickItem;
                TargetSlots.Add(s);
            }
        }


        for (int i = 0; i < PlayerNodeCount; i++)
        {
            InvSlot s = PlayerInventorySlotRoot.GetNode<InvSlot>(PlayerInventorySlotName + i);
            s.index = i;
            s.LeftClick += OnPlayerLeftClickItem;
            s.RightClick += OnPlayerRightClickItem;
            PlayerSlots.Add(s);
        }
    }

    public override void _Process(double delta)
    {
        if (PlayerNode?.playerData?.Inventory == null) return;
        var item = PlayerNode?.playerData?.Inventory.GetHandItemStack();
        if (item != null)
        {
            HandItem.Visible = true;
            HandItem.Position = GetViewport().GetMousePosition();
        }
        else
        {
            HandItem.Visible = false;
        }
    }

    public void OnPlayerLeftClickItem(int index, bool Shift)
    {
        OnPlayerButtonPressed(index,true, Shift);
    }
    public void OnPlayerRightClickItem(int index, bool Shift)
    {
        OnPlayerButtonPressed(index, false,Shift);
    }
    public void OnPlayerButtonPressed(int index, bool isLeft, bool isShift)
    {
        if (PlayerNode?.world == null) return;
        if (PlayerNode?.playerData?.Inventory == null) return;

        int type = 0;
        if (!isLeft) type = 1;

        var events = PlayerNode.world.Service.PlayerService.Events;
        var ppi = new PlayerPickItemEvent()
        {
            world = PlayerNode.world,
            Player = PlayerNode.playerData,
            Inventory = PlayerNode.playerData.Inventory,
            Index = index,
            ActionType = type,
        };


        if (isShift)
        {
            var targetinv = TargetBlock?.GetComponent<InventoryComponent>()?.GetInventory();
            if (targetinv == null)
            {
                events.PickItem(ppi);
                if (index < 9)
                {
                    ppi.Index = PlayerNode.playerData.Inventory.GetEmpyIndex(9);
                    events.PickItem(ppi);
                }
                else
                {
                    ppi.Index = PlayerNode.playerData.Inventory.GetEmpyIndex(0);
                    events.PickItem(ppi);
                }
            }
            else
            {
                events.PickItem(ppi);
                ppi.Index = targetinv.GetEmpyIndex();
                ppi.Inventory = targetinv;
                events.PickItem(ppi);
            }
        }
        else
        {
            events.PickItem(ppi);
        }
    }

    public void OnTargetButtonPressed(int index, bool isLeft, bool isShift)
    {
        if (PlayerNode?.world == null) return;
        if (PlayerNode?.playerData?.Inventory == null) return;

        int type = 0;
        if (!isLeft) type = 1;


        var ppi = new PlayerPickItemEvent()
        {
            world = PlayerNode.world,
            Player = PlayerNode.playerData,
            Inventory = TargetBlock?.GetComponent<InventoryComponent>()?.GetInventory(),
            Index = index,
            ActionType = type,
        };


        if (isShift)
        {
            PlayerNode.world.Service.PlayerService.Events.PickItem(ppi);
            ppi.Inventory = PlayerNode.playerData.Inventory;
            ppi.Index = ppi.Inventory.GetEmpyIndex();
            PlayerNode.world.Service.PlayerService.Events.PickItem(ppi);
        }
        else
        {
            PlayerNode.world.Service.PlayerService.Events.PickItem(ppi);
        }
    }

    public void UpdateGui()
    {
        if (PlayerNode != null) PlayerNode.Visible = Visible;
        if (PlayerNode?.playerData?.Inventory != null)
        {
            if (PlayerNode?.playerData?.Inventory.GetHandItemStack() != null)
                HandItem.Texture = PlayerNode?.playerData?.Inventory.GetHandItemStack().GetItemMeta().GetTexture(0);

            for (int i = 0; i < PlayerNodeCount; i++)
            {
                if (i < PlayerSlots.Count)
                {
                    var item = PlayerNode?.playerData?.Inventory.GetItem(i);
                    PlayerSlots[i].SetShowItem(item);
                }
                else
                {
                    GD.PrintErr($"[容器节点错误]缺失 Slot！当前节点数:{PlayerSlots.Count},玩家库存大小:{PlayerNodeCount}");
                }
            }
        }

        if (TargetBlock != null)
        {
            for (int i = 0; i < TargetNodeCount; i++)
            {
                var item = TargetBlock?.GetComponent<InventoryComponent>()?.GetInventory()?.GetItem(i);
                if (i < TargetSlots.Count)
                    TargetSlots[i].SetShowItem(item);
                else GD.PrintErr($"[容器节点错误]缺失 Slot！当前节点数:{PlayerSlots.Count},目标库存大小:{TargetNodeCount}");
            }
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        if (!HasNode("MarginContainer"))
            return ["缺少子节点! MarginContainer"];
        return [];
    }
}