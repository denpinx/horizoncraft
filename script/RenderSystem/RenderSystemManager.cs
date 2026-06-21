using System.Collections.Generic;
using Horizoncraft.script.RenderSystem.render;

namespace Horizoncraft.script.RenderSystem;

/// <summary>
/// 绘制系统管理器,没有组件，只有系统,无状态。
/// </summary>
public static class RenderSystemManager
{
    private static List<RenderSystem> RenderSystems = [];

    /// <summary>
    /// 渲染时调用
    /// </summary>
    /// <param name="id">id</param>
    /// <returns></returns>
    public static RenderSystem GetRender(int id)
    {
        if (id >= RenderSystems.Count) return null;
        return RenderSystems[id];
    }

    /// <summary>
    /// 仅在方块元数据在程序加载时调用
    /// </summary>
    /// <param name="renderName">渲染系统名</param>
    /// <returns></returns>
    public static int GetRenderId(string renderName)
    {
        for (int i = 0; i < RenderSystems.Count; i++)
            if (renderName == RenderSystems[i].Name)
                return i;
        return -1;
    }

    public static void RegisterRenderSystem(RenderSystem system)
    {
        RenderSystems.Add(system);
    }

    static RenderSystemManager()
    {
        //测试绘制
        RegisterRenderSystem(new TestRender());
        //水流动绘制组
        RegisterRenderSystem(new WaterRender());
        //芦苇生长绘制
        RegisterRenderSystem(new ReedRender());
        //水缸绘制
        RegisterRenderSystem(new TankRender());
        //管道链接绘制
        RegisterRenderSystem(new FluidPipeRender());
    }
}