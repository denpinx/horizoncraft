using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Entity
{
    public partial class EntityNode : RigidBody2D
    {
        public World world;
        public bool physics = true;
        public Entitydata Data = new Entitydata();
        public int ID;
        public override void _PhysicsProcess(double delta)
        {
            Data.position.X = GlobalPosition.X;
            Data.position.Y = GlobalPosition.Y;

            if (physics)
            {
                if (world != null && world.HasTileMap(Data.ChunkCoord))
                {
                    Freeze = false;
                }
                else
                {
                    Freeze = true;
                }
            }
        }

        public override void _EnterTree()
        {
            if (Data != null) GlobalPosition = new(Data.position.X, Data.position.Y);
            if (world != null && world.HasTileMap(Data.ChunkCoord))
            {
                Freeze = false;
            }
            else
            {
                Freeze = true;
            }
        }
    }
}
