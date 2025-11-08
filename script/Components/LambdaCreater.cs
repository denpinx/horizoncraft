#define DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Godot;
using Dict = System.Collections.Generic.Dictionary<string, object>;
using Expression = System.Linq.Expressions.Expression;


namespace Horizoncraft.script.Components
{
    /// <summary>
    /// 用于使用集合配置来构建含有初始化配置的对象，获取极致的可配置性，以及高性能。
    /// </summary>
    public static class LambdaCreater
    {
        const string TypeNamespace = "Horizoncraft.script.Components";

        /// <summary>
        /// 获取对象的表达式
        /// </summary>
        /// <param name="FromObject">对象，只能是 list<object> 和 Dictionary<string, object> 和一些基本类型</param>
        /// <returns></returns>
        private static Expression GetObjcetExpression(object FromObject)
        {
            if (FromObject is List<object> ls)
            {
                List<Expression> items = new();
                foreach (var item in ls)
                {
                    items.Add(GetObjcetExpression(item));
                }

                var exp = Expression.ListInit(Expression.New(ls.GetType()), items.ToArray<Expression>());
                return exp;
            }

            if (FromObject is Dictionary<string, object> map)
            {
                var addmethod = map.GetType().GetMethod("Add");
                List<ElementInit> items = new();
                foreach (var key in map.Keys)
                {
                    var value = map[key];
                    items.Add(
                        Expression.ElementInit(addmethod, Expression.Constant(key),
                            GetObjcetExpression(value))
                    );
                }

                var exp = Expression.ListInit(Expression.New(map.GetType()), items.ToArray());
                return exp;
            }

            //非集合类型
            return Expression.Constant(FromObject);
        }

        /// <summary>
        /// 
        /// 通过字典和表达式创建lambda，支持内部的字典和List嵌套，
        /// 字典内部集合类型只支持 list<string> 和 dictionar<string,objcet> 两个集合
        /// 
        /// 因为字典会导致字段和类型名无法分辩，除非添加自定义的类型/类名解析，但是会增加复杂度。
        ///
        /// 
        /// 列一: ()=>new TypeName{属性};
        ///
        /// 列二: ()=>new TypeName{list = new List<string>(){
        ///         "aa","bb","cc"
        ///     },//这里的list类型会根据主类型的字段类型的实际情况自动转换
        ///     dict = new dictionary<string,objcet>(){
        ///         {"a","a"},
        ///         {"b","a"},
        ///         {"list",new list(){...}},
        ///     }  //这里也是字典的值也会根据真实情况自动转换
        /// };
        /// 
        /// 用labmda构建对象只会相比原生new对象慢了20%。
        /// 
        /// </summary>
        /// <param name="typename">类型名</param>
        /// <param name="cfg">配置字典</param>
        /// <param name="lowercase">是否忽略大小写</param>
        /// <returns></returns>
        public static Func<T> CreateLambda<T>(string typename, Dict cfg, bool lowercase = false)
        {
            Type type = FindTypesInNamespaceGlobally(typename, TypeNamespace);
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

                bindings.Add(Expression.Bind(member, GetObjcetExpression(ResultValue)));
            }

            var body = Expression.MemberInit(newExpr, bindings);
            var lambda = Expression.Lambda<Func<T>>(body);

#if DEBUG
            GD.Print(
                $"[{nameof(LambdaCreater)}]\n" +
                $"\tCreateLambda :\t {lambda} \n " +
                $"\tFromJson: \n{System.Text.Json.JsonSerializer.Serialize(cfg, options: new System.Text.Json.JsonSerializerOptions()
                {
                    WriteIndented = true
                })}\n");
#endif

            var result = lambda.Compile();

            //第一次调用lambda会非常耗时，可能是因为jit的原因，所以我先在这里调用一次之后就不会出现耗时问题了。
            result();
            return result;
        }

        /// <summary>
        /// 获取命名空间下的指定名称的类型.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="namespacePrefix"></param>
        /// <returns></returns>
        public static Type FindTypesInNamespaceGlobally(
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

            if (results.Count == 0)
            {
                GD.PrintErr($"未找到：{typeName}");
            }
            return results?.First();
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

                if (string.Equals(fi.Name, name, StringComparison.CurrentCultureIgnoreCase))

                    return fi;


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
                //GD.Print(fi.Name, "==", name);
                if (string.Equals(fi.Name, name, StringComparison.CurrentCultureIgnoreCase)) return fi;
            }

            return null;
        }
    }
}