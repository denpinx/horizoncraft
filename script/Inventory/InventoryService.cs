using HorizonCraft.script.WorldControl.Service;

namespace horizoncraft.script.Inventory;

public class InventoryService
{
    private WorldBase WorldService;

    public InventoryService(WorldBase worldBase)
    {
        this.WorldService = worldBase;
    }
}