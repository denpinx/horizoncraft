using Godot;
using Horizoncraft.script.Components.Item;
using Horizoncraft.script.Inventory;

public partial class Slot : Control
{
    public ItemStack ShowItem;
    public bool LastHoverState = false;
    [Export]public TextureRect TextureRect_Item;
    [Export]public TextureRect TextureRect_Selected;
    [Export]public Label Label_Amount;
    [Export]public TextureProgressBar ProgressBar;
    public void SetShowItem(ItemStack itemStack)
    {
        ShowItem = itemStack;
        if (itemStack != null)
        {
            this.TextureRect_Item.Texture = itemStack.GetItemMeta().GetTexture(itemStack.State);
            this.Label_Amount.Text = itemStack.Amount.ToString();
            var durable = itemStack.GetComponent<ItemDurableComponent>();
            if (durable != null)
            {
                ProgressBar.Value = durable.Value;
                ProgressBar.MaxValue = durable.Max;
                ProgressBar.Visible = true;
            }
            else
            {
                ProgressBar.Visible = false;
            }
        }
        else
        {
            this.TextureRect_Item.Texture = null;
            this.Label_Amount.Text = "";
            ProgressBar.Visible = false;
        }
    }

    public void OnHover()
    {
        if (!LastHoverState)
        {
            TextureRect_Selected.Visible = true;
            LastHoverState = true;
        }
    }

    public void OnUnhover()
    {
        if (LastHoverState)
        {
            TextureRect_Selected.Visible = false;
            LastHoverState = false;
        }
    }
}