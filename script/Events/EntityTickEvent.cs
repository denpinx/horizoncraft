using System;
using Horizoncraft.script.Components.EntityComponents;
using Horizoncraft.script.Entity;
using Horizoncraft.script.Services.world;

namespace Horizoncraft.script.Events;

public class EntityTickEvent
{
    public WorldServiceBase WorldService;
    public EntityComponent EntityComponent;
    public EntityData EntityData;
    public Guid UUID;

    public T GetComponent<T>() where T : EntityComponent
    {
        return (T)EntityComponent;
    }
}