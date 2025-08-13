using Godot;
using horizoncraft.script.WorldControl.work;

namespace horizoncraft.script.WorldControl
{
    public class BlockStrcut
    {
        public SetBlockWork work = new SetBlockWork();
        public (BlockMeta, int) GetBlocMeta(int X, int Y, int Z)
        {
            for (int i = 0; i < work.ExclList.Count; i++)
            {
                var item = work.ExclList[i];
                if (item.Item1.X == X && item.Item1.Y == Y && item.Item1.Z == Z)
                    return (item.Item2, item.Item3);
            }
            return (null, 0);
        }
    }
}