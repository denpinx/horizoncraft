using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Godot;
using Dict = System.Collections.Generic.Dictionary<string, object>;
using Expression = System.Linq.Expressions.Expression;

namespace horizoncraft.script.Components
{
    public class LambdaCreater
    {
        public static Func<Component> CreateLambda(string typename, Dict cfg)
        {
            string Typenamespace = "horizoncraft.script.Components";
            Type type = null;
            List<Type> types = FindTypesInNamespaceGlobally(typename, Typenamespace);
            if (types.Count > 0) type = types.First();
            if (type == null)
            {
                GD.PrintErr($"{Typenamespace + "." + typename} 不存在!");
                return null;
            }

            var newExpr = Expression.New(type);
            var bindings = new List<MemberBinding>();
            foreach (var kv in cfg)
            {
                var key = kv.Key.ToString();
                var value = kv.Value;

                var member = (MemberInfo)type.GetField(key) ?? type.GetProperty(key);
                if (member == null) continue;

                var targetType = member switch
                {
                    FieldInfo f => f.FieldType,
                    PropertyInfo p => p.PropertyType,
                    _ => null
                };
                if (targetType == null) continue;

                //防止输入类型和实际类型不符
                object ResultValue = value;
                if (value.GetType() != targetType)
                {
                    ResultValue = Convert.ChangeType(value, targetType);
                }

                bindings.Add(Expression.Bind(member, Expression.Constant(ResultValue, targetType)));
            }

            var body = Expression.MemberInit(newExpr, bindings);
            var lambda = Expression.Lambda<Func<Component>>(body);
            //GD.Print(lambda.ToString());
            return lambda.Compile();
        }

        public static List<Type> FindTypesInNamespaceGlobally(
            string typeName,
            string namespacePrefix)
        {
            var results = new List<Type>();
            string prefix = namespacePrefix.EndsWith(".") ? namespacePrefix : namespacePrefix + ".";

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.Name == typeName &&
                                    t.Namespace != null &&
                                    (t.Namespace == namespacePrefix || t.Namespace.StartsWith(prefix)));
                    results.AddRange(types);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    var types = ex.Types
                        .Where(t => t != null &&
                                    t.Name == typeName &&
                                    t.Namespace != null &&
                                    (t.Namespace == namespacePrefix || t.Namespace.StartsWith(prefix)));
                    results.AddRange(types);
                }
            }

            return results;
        }
    }
}