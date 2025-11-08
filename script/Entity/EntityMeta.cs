using Godot;

namespace Horizoncraft.script.Entity
{
    public class EntityMeta
    {
        public delegate Node2D GetEntityNodelegate(PackedScene packedScene);

        public GetEntityNodelegate get_entity_node;
        public PackedScene PackedScene = new();
        public string Name;
        public int Id;

        public EntityMeta(string name, string scenepath)
        {
            PackedScene = GD.Load<PackedScene>(scenepath);
            this.Name = name;
        }

        public IEntityNode GetEntityNode()
        {
            IEntityNode entityNode = PackedScene.Instantiate() as IEntityNode;
            entityNode.Id = Id;
            return entityNode;
        }
    }
}