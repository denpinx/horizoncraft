using System;
using horizoncraft.script.Components;

namespace horizoncraft.script.Inventory;

public abstract class InventoryPatternMatching
{
    public abstract bool CanProcess(InventoryComponent c);
    public abstract void Yes(InventoryComponent c);
    public abstract void No(InventoryComponent c);
}

// 非泛型基类
public class InventoryPatternMatcher<T> : InventoryPatternMatching where T : InventoryComponent
{
    public Func<T, bool> CanProcessTyped;
    public Action<T> YesTyped;
    public Action<T> NoTyped;

    public override bool CanProcess(InventoryComponent c)
    {
        return CanProcessTyped(c as T);
    }

    public override void Yes(InventoryComponent c)
    {
        YesTyped(c as T);
    }

    public override void No(InventoryComponent c)
    {
        NoTyped(c as T);
    }
}