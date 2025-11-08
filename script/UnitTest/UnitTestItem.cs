using System.Diagnostics;
using Godot;

namespace Horizoncraft.script.UnitTest;

/// <summary>
/// 单元测试项
/// </summary>
/// <typeparam name="Result">测</typeparam>
public class UnitTestItem<Result> : IUnitTest
{
    /// <summary>
    /// 匹配测试结果
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public virtual bool MatchResult(Result result)
    {
        return true;
    }

    /// <summary>
    /// 开始测试
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    protected virtual Result StartTest(Node node)
    {
        return default(Result);
    }

    public string Start(Node node, string UnitName)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        var result = StartTest(node);
        var is_pass = MatchResult(result);
        stopwatch.Stop();
        if (is_pass)
            return
                $"{UnitName,-8} {stopwatch.Elapsed.TotalMilliseconds,-5} ms,{stopwatch.Elapsed.TotalMicroseconds,-5} μs - #通过";
        return
            $"{UnitName,-8} {stopwatch.Elapsed.TotalMilliseconds,-5} ms,{stopwatch.Elapsed.TotalMicroseconds,-5} μs - #失败";
    }
}