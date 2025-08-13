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
        public EntityNode GetEntityNode()
        {
            EntityNode entityNode = get_entity_node(packedScene) as EntityNode;
            entityNode.ID = id;
            return entityNode;
        }
        public virtual Entitydata GetData(Vector2 vector2)
        {
            return new Entitydata()
            {
                id = id,
                position = new(vector2.X, vector2.Y)
            };
        }
    }
}
