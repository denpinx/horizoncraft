using horizoncraft.script.Components.EntityComponents;
using horizoncraft.script.Entity;
using HorizonCraft.script.Services.world;

namespace horizoncraft.script.Events.SystemEvents;

public class EntitySystemEvent
{
    public WorldServiceBase Service;
    public EntityData EntityData;
    public EntityComponent EntityComponent;
}