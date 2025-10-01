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
    public static class LambdaCreater
    {
        const string TypeNamespace = "horizoncraft.script.Components";

        /// <summary>
        /// 通过表达式创建lambda
        /// 内容为: ()=>new TypeName{属性};
        /// 除了第一次调用之外，之后的调用都会接近原生创建对象
        /// </summary>
        /// <param name="typename">类型名</param>
        /// <param name="cfg">属性字典</param>
        /// <returns>lambda表达式</returns>
        public static Func<Component> CreateLambda(string typename, Dict cfg, bool lowercase = false)
        {
            Type type = null;
            List<Type> types = FindTypesInNamespaceGlobally(typename, TypeNamespace);
            if (types.Count > 0) type = types.First();
            if (type == null)
            {
                GD.PrintErr($"{typename} 不存在于命名空间{TypeNamespace}中");
                return null;
            }

            var newExpr = Expression.New(type);
            var bindings = new List<MemberBinding>();
            foreach (var kv in cfg)
            {
                var key = kv.Key.ToString();
                var value = kv.Value;
                MemberInfo member = null;
                if (lowercase)
                {
                    member = (MemberInfo)type.GetLowercaseField(key) ?? type.GetLowercaseProperty(key);
                    if (member == null)
                    {
                        member = (MemberInfo)type.GetField(key) ?? type.GetProperty(key);
                    }
                }
                else
                {
                    member = (MemberInfo)type.GetField(key) ?? type.GetProperty(key);
                    if (member == null)
                    {
                        member = (MemberInfo)type.GetLowercaseField(key) ?? type.GetLowercaseProperty(key);
                    }
                }

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
            GD.Print(lambda.ToString());
            var result = lambda.Compile();
            _ = result();
            return result;
        }

        /// <summary>
        /// 获取命名空间下的指定名称的类型，非常耗时！ 150ms左右
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="namespacePrefix"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 通过小写获取字段,你首先得先确定你的变量名没有别的小写变体
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="name">变量名</param>
        /// <returns></returns>
        public static FieldInfo GetLowercaseField(this Type type, string name)
        {
            foreach (var fi in type.GetFields())
            {
                GD.Print(fi.Name, "==", name);
                if (string.Equals(fi.Name, name, StringComparison.CurrentCultureIgnoreCase)) return fi;
            }

            return null;
        }

        /// <summary>
        /// 通过小写获取属性,你首先得先确定你的变量名没有别的小写变体
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="name">变量名</param>
        /// <returns></returns>
        public static PropertyInfo GetLowercaseProperty(this Type type, string name)
        {
            foreach (var fi in type.GetProperties())
            {
                GD.Print(fi.Name, "==", name);
                if (string.Equals(fi.Name, name, StringComparison.CurrentCultureIgnoreCase)) return fi;
            }

            return null;
        }
    }
}