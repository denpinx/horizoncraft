using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Vector2 = Godot.Vector2;

namespace horizoncraft.script.Share
{
    public class PlayerData
    {
        public Player player;
        public Vector2I Coord;
        public Vector2I ChunkCoord;
        public String Name;
        public PlayerData()
        {
        }
        public Dictionary GetDictionary()
        {
            return new Dictionary()
            {
                {"Position.x",player.Position.X},
                {"Position.y",player.Position.Y},
                { "Coord.x",Coord.X},
                { "Coord.y",Coord.Y},
                {"ChunkCoord.x",ChunkCoord.X},
                {"ChunkCoord.y",ChunkCoord.Y},
            };
        }
        public void ParseDictionary(Dictionary dict)
        {
            this.Coord.X = (int)dict["Coord.x"];
            this.Coord.Y = (int)dict["Coord.y"];
            this.ChunkCoord.X = (int)dict["ChunkCoord.x"];
            this.ChunkCoord.Y = (int)dict["ChunkCoord.y"];
            Vector2 vector2 = new Godot.Vector2();
            vector2.X = (float)dict["Position.x"];
            vector2.Y = (float)dict["Position.y"];
            player.Position = vector2;
        }
    }
}
