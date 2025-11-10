using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Components.Interfaces;
using Horizoncraft.script.I18N;
using Horizoncraft.script.WorldControl;

namespace Horizoncraft.tscn.Gui;

/// <summary>
/// 方块信息视图
/// </summary>
public partial class BlockInfoView : Control
{
    private List<ProgressItem> _progressItems = new();
    private PackedScene _progressItemPack = GD.Load<PackedScene>("res://tscn/Gui/ProgressItem.tscn");
    [Export] private TextureRect _textureRect;
    [Export] private VBoxContainer _listNode;
    [Export] private Label _label;

    public override void _Ready()
    {
    }

    /// <summary>
    /// 更新视图展示的方块信息
    /// </summary>
    /// <param name="block">方块</param>
    public void SetBlockData(BlockData block)
    {
        if (block == null || block.IsMeta("air"))
        {
            Visible = false;
            return;
        }

        Visible = true;
        _label.Text = block.BlockMeta.Name.Trprefix("meta") + " 组件数:" + block.Components.Count + ",状态:" + block.State +
                      ",光照值:" + block.Light;
        _textureRect.Texture = block.BlockMeta.ItemMeta.GetTexture();
        int used = 0;
        foreach (var cmp in block.Components)
        {
            if (cmp is IGetProgress igp)
            {
                var pv = igp.GetProgress();
                if (_progressItems.Count <= used)
                {
                    //新建
                    var pi = _progressItemPack.Instantiate<ProgressItem>();
                    _progressItems.Add(pi);
                    _listNode.AddChild(pi);
                    pi.name = pv.Name;
                    pi.value = pv.Value;
                    pi.max = pv.Max;
                }
                else
                {
                    //复用旧的
                    _progressItems[used].name = pv.Name;
                    _progressItems[used].value = pv.Value;
                    _progressItems[used].max = pv.Max;
                }

                used++;
            }
        }

        //删除多余的
        for (int i = _progressItems.Count - 1; i >= used; i--)
        {
            _progressItems[i].QueueFree();
            _progressItems.RemoveAt(i);
        }
    }
}