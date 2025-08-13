using System;
using System.Collections.Generic;
using Godot;

using horizoncraft.script.Events;
using horizoncraft.script.WorldControl;

namespace horizoncraft.script.Components
{
    using System;
    //Link & Execut,ECS
    public class ComponentManager
    {
        private static readonly Random _Random = new Random();
        static Dictionary<string, ComponentAndSystem> CMPSets = new();
        public static void ExecuteComponents(WorldEvent worldEvent, Blockdata blockdata)
        {
            for (int i = 0; i < blockdata.components.Count; i++)
            {
                Component item = blockdata.components[i];
                if (item == null)
                {
                    GD.PrintErr("item is null");
                }
                else
                {
                    if (item.Name == null) GD.PrintErr($"{item.GetType()} name is null");
                }
                if (CMPSets.ContainsKey(item.Name))
                    CMPSets[item.Name].system.Execute(worldEvent, item);
            }

        }
        public static void Register<T>(String key, Func<Component> func, T System) where T : IComponentSystem
        {
            CMPSets.Add(key, new ComponentAndSystem()
            {
                GetComponect = func,
                system = System,
            });
        }

        static ComponentManager()
        {
            Register("BlockCover", () => new ExpandComponent(), new TickSystem()
            {
                Tick = (BlockTickEvent e, TickComponent cmp) =>
                {
                    var ec = cmp as ExpandComponent;
                    if (e.CheckIsCube(e.TopBlock)) e.Blockdata.SetMeta(Materials.Valueof(ec.BlockName));
                }
            ,
            });
            Register("BlockSpread", () => new ExpandComponent(), new TickSystem()
            {
                Tick = (BlockTickEvent e, TickComponent cmp) =>
                {
                    var ec = cmp as ExpandComponent;
                    var meta = Materials.Valueof(ec.BlockName);
                    if (e.CheckIsCube(e.TopBlock)) return;
                    if (e.CheckMeta(e.LeftBlock, meta) && _Random.Next(0, 5) < 1)
                    {
                        e.Blockdata.SetMeta(meta);
                    }
                    if (e.CheckMeta(e.RightBlock, meta) && _Random.Next(0, 5) < 1)
                    {
                        e.Blockdata.SetMeta(meta);
                    }
                }
            });
            Register("BottomCheck", () => new TickComponent(), new TickSystem()
            {
                Tick = (BlockTickEvent e, TickComponent cmp) =>
                {
                    if (!e.CheckIsCube(e.BottomBlock)) e.Blockdata.SetMeta(Materials.Valueof("air"));
                }
,
            });

            Register("FluidComponent", () => new FluidComponent(), new TickSystem()
            {
                Tick = (BlockTickEvent e, TickComponent cmp) =>
                {
                    FluidComponent fc = cmp as FluidComponent;
                    BlockMeta blockMeta = Materials.Valueof(fc.BlockName);

                    bool MoveFluid(Blockdata target)
                    {
                        if (e.CheckMeta(target, blockMeta))
                        {
                            if (target.STATE > e.Blockdata.STATE + 1)
                            {
                                target.GetComponent<FluidComponent>("FluidComponent").mobility = true;
                                target.STATE = e.Blockdata.STATE + 1;
                                return true;
                            }
                        }
                        if (e.CheckMeta(target, Materials.Valueof("air")))
                        {
                            target.SetMeta(blockMeta);
                            target.GetComponent<FluidComponent>("FluidComponent").mobility = true;
                            target.STATE = e.Blockdata.STATE + 1;
                            return true;
                        }
                        return false;
                    }
                    if (e.CheckMeta(e.TopBlock, blockMeta))
                    {
                        e.Blockdata.STATE = 0;
                    }
                    if (e.CheckMeta(e.BottomBlock, Materials.Valueof("air")))
                    {
                        e.BottomBlock.SetMeta(blockMeta);
                        e.BottomBlock.GetComponent<FluidComponent>("FluidComponent").mobility = true;
                    }
                    else
                    {
                        if (e.Blockdata.STATE >= 7) return;
                        MoveFluid(e.LeftBlock);
                        MoveFluid(e.RightBlock);
                    }

                },
            });
            //沙子模拟
            Register("PhysicsComponent", () => new PhysicsComponent(), new TickSystem()
            {
                Tick = (BlockTickEvent e, TickComponent cmp) =>
                {
                    PhysicsComponent pc = cmp as PhysicsComponent;
                    BlockMeta pcmeta = Materials.Valueof(pc.BlockName);
                    BlockMeta air = Materials.Valueof("air");
                    if (e.CheckMeta(e.BottomBlock, air))
                    {
                        e.BottomBlock.SetMeta(pcmeta);
                        e.Blockdata.SetMeta(air);
                        return;
                    }
                    else if (e.CheckMeta(e.TopBlock, pcmeta))
                    {
                        if (e.CheckMeta(e.LeftBlock, air))
                        {
                            e.LeftBlock.SetMeta(pcmeta);
                            e.Blockdata.SetMeta(air);
                            return;
                        }
                        else if (e.CheckMeta(e.RightBlock, air))
                        {
                            e.RightBlock.SetMeta(pcmeta);
                            e.Blockdata.SetMeta(air);
                            return;
                        }
                    }
                }
            });
        }
    }
}