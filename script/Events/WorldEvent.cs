using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Godot;
using horizoncraft.script.Components;
using horizoncraft.script.WorldControl;
using HorizonCraft.script.WorldControl.Service;

namespace horizoncraft.script.Events
{
    public class WorldEvent
    {
        public PlayerData Player;
        public World World;
        public WorldBase WorldService;
    }
}