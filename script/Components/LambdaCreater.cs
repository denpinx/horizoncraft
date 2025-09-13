using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using horizoncraft.script.Components.EnergyBlocks;
using horizoncraft.script.Components.Item;
using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace horizoncraft.script.Components
{
    public class LambdaCreater
    {
        public static readonly Dictionary<string, Func<Dict, Component>> _factories = new();

        static LambdaCreater()
        {
            Register<TickComponent>();
            Register<ItemComponent>();
            Register<ExpandComponent>();
            Register<FluidComponent>();
            Register<PhysicsComponent>();
            Register<InventoryComponent>();
            Register<FurnaceComponent>();
            Register<ItemDurableComponent>();
            Register<EnergyUnitComponent>();
        }

        public static void Register<T>() where T : Component, new()
        {
            _factories[typeof(T).Name] = cfg =>
            {
                var newExpr = Expression.New(typeof(T));
                var bindings = new List<MemberBinding>();

                foreach (var kv in cfg)
                {
                    var key = kv.Key.ToString();
                    var value = kv.Value;

                    var member = (MemberInfo)typeof(T).GetField(key) ?? typeof(T).GetProperty(key);
                    if (member == null) continue;

                    var targetType = member switch
                    {
                        FieldInfo f => f.FieldType,
                        PropertyInfo p => p.PropertyType,
                        _ => null
                    };
                    if (targetType == null) continue;
                    bindings.Add(Expression.Bind(member, Expression.Constant(value, targetType)));
                }

                var body = Expression.MemberInit(newExpr, bindings);
                var lambda = Expression.Lambda<Func<Dict, Component>>(body, Expression.Parameter(typeof(Dict), "cfg"));
                return lambda.Compile()(cfg);
            };
        }

        public static Func<Component> CreateLambda(string typeName, Dict cfg)
        {
            if (!_factories.TryGetValue(typeName, out var factory))
                throw new ArgumentException($"未注册的组件类型: {typeName}");

            return () => factory(cfg);
        }
    }
}