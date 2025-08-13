using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace horizoncraft.script.WorldControl
{
    public class BlockTileSet
    {
        //state：   什么状态时使用这个tile_id
        //tile_id:  当前状态的tile_id
        //tile_size:当前图集的大小,必须为1:1
        //tile_count:当前图集的图块数
        public int state = 0;
        public int tile_id;
        public int tile_size;
        public int tile_count;

        public string texture_name;
    }
}