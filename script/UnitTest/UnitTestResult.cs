using System;
using MemoryPack;

namespace Horizoncraft.script.UnitTest;

[MemoryPackable]
public partial class UnitTestResult
{
    /// <summary>
    /// 测试名称
    /// </summary>
    public required String TestName;

    /// <summary>
    /// 测试耗时，单位:μs
    /// </summary>
    public required double UseTime;

    /// <summary>
    /// 测试日期
    /// </summary>
    public required DateTime TestTime;

    public required string ResultText;
}