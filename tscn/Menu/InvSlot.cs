using Godot;
using System;
using System.Text;
using horizoncraft.script.Components.Item;
using horizoncraft.script.I18N;
using horizoncraft.script.Inventory;

public partial class InvSlot : Control
{
    [Export] public bool HideBackGround = false;
    public Action<int, bool, bool> LeftClick;
    public Action<int, bool, bool> RightClick;
    public int index = 0;
    public ItemStack ShowItem;
    public bool LastHoverState = false;
    public TextureRect TextureRect_Item;
    public Label Label_Amount;
    public AnimationPlayer AnimationPlayer;
    public TextureButton TextureButton;
    public TextureProgressBar ProgressBar;
    bool LastFrame = false;
    bool InputActive = false;
    double locktime = 0;

    public override void _Ready()
    {
        TextureRect_Item = GetNode<TextureRect>("TextureRect_Item");
        ProgressBar = GetNode<TextureProgressBar>("TextureProgressBar");
        Label_Amount = GetNode<Label>("Label_Amount");
        TextureButton = GetNode<TextureButton>("TextureButton");
        if (HideBackGround)
        {
            GetNode<TextureRect>("TextureRect_Font").Visible = false;
        }

        SetProcessInput(true);
    }

    public override void _Process(double delta)
    {
        if (InputActive)
        {
            InputActive = false;
        }
        else
        {
            LastFrame = false;
        }

        if (locktime > 0) locktime -= delta;
        else locktime = 0;
    }

    public override void _Input(InputEvent @event)
    {
        InputActive = true;
        if (!IsVisibleInTree()) return;
        Vector2 globalMousePos = GetViewport().GetMousePosition();
        Rect2 globalRect = GetGlobalRect();

        bool isInside = globalRect.HasPoint(globalMousePos);

        if (@event is InputEventMouseMotion || @event is InputEventMouseButton)
        {
            bool isRightPressed = Input.IsMouseButtonPressed(MouseButton.Right);
            bool isLeftPressed = Input.IsMouseButtonPressed(MouseButton.Left);

            if (isInside)
            {
                if (isRightPressed)
                {
                    if (!LastFrame && locktime == 0)
                    {
                        Vector2 localPos = globalMousePos - globalRect.Position;
                        if (Input.IsKeyPressed(Key.Shift))
                            RightClick?.Invoke(index, false, true);
                        else
                            RightClick?.Invoke(index, false, false);

                        locktime = 0.2f;
                        LastFrame = true;
                    }
                }
                else if (isLeftPressed)
                {
                    if (!LastFrame && locktime == 0)
                    {
                        if (Input.IsKeyPressed(Key.Shift))
                            LeftClick?.Invoke(index, true, true);
                        else
                            LeftClick?.Invoke(index, true, false);
                        locktime = 0.2f;
                        LastFrame = true;
                    }
                }
                else LastFrame = false;
            }
        }
    }


    public void SetShowItem(ItemStack itemStack)
    {
        ShowItem = itemStack;
        if (itemStack != null)
        {
            this.TextureRect_Item.Texture = itemStack.GetItemMeta().GetTexture(itemStack.State);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("tip_item_name".Trprefix("ui", itemStack.GetItemMeta().Name.Trprefix("meta")));

            if (itemStack.Amount > 1)
                this.Label_Amount.Text = itemStack.Amount.ToString();
            else this.Label_Amount.Text = "";

            var durable = itemStack.GetComponent<ItemDurableComponent>();
            if (durable != null)
            {
                if (durable.Max != durable.Value)
                {
                    ProgressBar.MaxValue = durable.Max;
                    ProgressBar.Value = durable.Value;
                    ProgressBar.Visible = true;
                }
                else
                {
                    ProgressBar.Visible = false;
                }

                stringBuilder.AppendLine("tip_item_tool_level".Trprefix("ui", durable.ToolLevel));
                stringBuilder.AppendLine("tip_item_tool_type".Trprefix("ui", durable.Tag));
                stringBuilder.AppendLine("tip_item_tool_durable".Trprefix("ui", durable.Value, durable.Max));
            }
            else
            {
                ProgressBar.Visible = false;
            }

            foreach (var kvp in itemStack.GetItemMeta().Tags)
            {
                stringBuilder.AppendLine($"[{kvp.Key} : {kvp.Value}]");
            }

            TextureButton.TooltipText = stringBuilder.ToString();
        }
        else
        {
            this.TextureRect_Item.Texture = null;
            this.Label_Amount.Text = "";
            TextureButton.TooltipText = "";
            ProgressBar.Visible = false;
        }
    }
}