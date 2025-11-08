using Horizoncraft.script.Components.EntityComponents;
using Horizoncraft.script.Entity;
using HorizonCraft.script.Services.world;

namespace Horizoncraft.script.Events.SystemEvents;

public class EntitySystemEvent
{
    public WorldServiceBase Service;
    public EntityData EntityData;
    public EntityComponent EntityComponent;
}