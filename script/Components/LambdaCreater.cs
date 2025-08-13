using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Dict = System.Collections.Generic.Dictionary<string, object>;
namespace horizoncraft.script.Components
{
    //AI 生成 ---
    public class LambdaCreater
    {
        public static readonly Dictionary<string, Func<Dict, Component>> _factories = new();
        static LambdaCreater()
        {
            Register<TickComponent>();
            Register<ExpandComponent>();
            Register<FluidComponent>();
            Register<PhysicsComponent>();
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

                    var converted = ConvertGodotValue(value, targetType);
                    bindings.Add(Expression.Bind(member, Expression.Constant(converted, targetType)));
                }

                var body = Expression.MemberInit(newExpr, bindings);
                var lambda = Expression.Lambda<Func<Dict, Component>>(body, Expression.Parameter(typeof(Dict), "cfg"));
                //Godot.GD.Print($"[Lambda] {lambda}");
                return lambda.Compile()(cfg);
            };
        }

        /* ---------- 根据字符串类名生成 lambda ---------- */
        public static Func<Component> CreateLambda(string typeName, Dict cfg)
        {
            if (!_factories.TryGetValue(typeName, out var factory))
                throw new ArgumentException($"未注册的组件类型: {typeName}");

            return () => factory(cfg);
        }

        /* ---------- 把 Godot Variant 转成 C# 值 ---------- */
        //bug : json转字典，字典 godot var 转 c# 原本的int值已经变成double了！ 所有 double都被转变成0了
        private static object ConvertGodotValue(object v, Type target)
        {
            if (v == null) return target.IsValueType ? Activator.CreateInstance(target) : null;

            // 1) 先把 Godot.Variant 拆箱成 C# 原始类型
            var unboxed = v switch
            {
                string s => (string)s,
                int i => (object)i,
                float f => (int)f,
                bool b => (object)b,
                double d => (double)d,
                // 其他常用类型按需继续补
                _ => v.ToString()   // 兜底，防止 Vector2/Color 等复杂 Variant
            };

            // 2) 再按目标类型转换
            return unboxed switch
            {
                string s when target == typeof(string) => s,
                string s when target == typeof(int) => int.TryParse(s, out var i) ? i : 0,
                string s when target == typeof(float) => float.TryParse(s, out var f) ? f : 0f,

                int i when target == typeof(int) => i,
                int i when target == typeof(float) => (float)i,

                float f when target == typeof(float) => f,
                float f when target == typeof(int) => (int)f,


                double d when target == typeof(int) => (int)d,
                double d when target == typeof(float) => (float)d,
                bool b when target == typeof(bool) => b,

                _ => throw new NotSupportedException(
                        $"不支持把 {v}({v.GetType()}) 转成 {target}")
            };
        }
    }
}