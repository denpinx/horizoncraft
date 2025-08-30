using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl;
using horizoncraft.script.WorldControl.Service;
using HorizonCraft.script.WorldControl.Service;

namespace horizoncraft.script.Entity
{
    public partial class EntityNode : RigidBody2D
    {
        public World world;
        public bool physics = true;
        public Entitydata Data = new Entitydata();
        public int ID;

        public bool Moveed = false;
        private Vector2 LastPosition = Vector2.Zero;

        public override void _PhysicsProcess(double delta)
        {
            if (world == null) return;
            if (
                world.WorldService is WorldHostService ||
                world.WorldService is WorldSingleService
            )
            {
                Data.Position.X = GlobalPosition.X;
                Data.Position.Y = GlobalPosition.Y;
            }
            else if (world.WorldService is WorldClientService)
            {
                GlobalPosition = GlobalPosition = new Vector2(Data.Position.X, Data.Position.Y);
            }


            if (LastPosition != GlobalPosition) Moveed = true;
            LastPosition = GlobalPosition;


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
            if (Data != null) GlobalPosition = new(Data.Position.X, Data.Position.Y);
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