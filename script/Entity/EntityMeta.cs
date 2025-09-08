using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using horizoncraft.script.Events;

namespace horizoncraft.script.Entity
{
    public class EntityMeta
    {
        public delegate Node2D GetEntityNodelegate(PackedScene packedScene);

        public GetEntityNodelegate get_entity_node;
        public PackedScene packedScene = new();
        public string NAME;
        public int id;

        public EntityMeta(string name, string scenepath)
        {
            packedScene = GD.Load<PackedScene>(scenepath);
            this.NAME = name;
        }

        public IEntityNode GetEntityNode()
        {
            IEntityNode entityNode = get_entity_node(packedScene) as IEntityNode; 
            entityNode.Id = id;
            return entityNode;
        }
    }
}