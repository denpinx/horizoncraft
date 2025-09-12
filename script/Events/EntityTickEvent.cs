using System;
using horizoncraft.script.Components.EntityComponents;
using horizoncraft.script.Entity;
using HorizonCraft.script.Services.world;

namespace horizoncraft.script.Events;

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