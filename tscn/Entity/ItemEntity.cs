using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.Entity;

namespace horizoncraft.script.Events
{
    public partial class ItemEntity : EntityNode
    {
        Label label;
        public override void _Ready()
        {
            base._Ready();
            label = GetNode<Label>("Label");
        }
        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            label.Text =
            @$"
               Position      : {Position}
               GlobalPosition:  {GlobalPosition}
               Coord         : {Data.Coord}
               ChunkCoord    : {Data.ChunkCoord}
            ";
        }
    }
}