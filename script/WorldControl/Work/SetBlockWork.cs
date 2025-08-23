using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.WorldControl.work
{
    public class SetBlockWork : WorkBase
    {
        public SetBlockWork()
        {
            Type = "SetBlock";
        }

        public List<(Vector3I, BlockMeta, int)> ExclList = new List<(Vector3I, BlockMeta, int)>();

        public override void Execute(Chunk chunk)
        {
            foreach (var item in ExclList)
            {
                chunk.SetBlock(item.Item1.X, item.Item1.Y, item.Item1.Z, item.Item2.Blockdata(), item.Item3);
            }
        }
    }
}