using Godot;
using Horizoncraft.script;

/// <summary>
/// 显示玩家手中的物品
/// </summary>
public partial class Body : Sprite2D
{
    PlayerNode _playerNode;
    private TextureRect _itemTexture;

    public override void _Ready()
    {
        _itemTexture = GetNode<TextureRect>("Left_arm/TextureRect");
        _playerNode = (PlayerNode)GetParent();
    }


    public override void _Process(double delta)
    {
        if (_playerNode != null && _playerNode.playerData != null)
        {
            var item = _playerNode.playerData.Inventory.GetToolBarItem();
            if (item != null)
            {
                _itemTexture.Visible = true;
                _itemTexture.Texture = item.GetItemMeta().GetTexture();
            }
            else
            {
                _itemTexture.Visible = false;
            }
        }
    }
}