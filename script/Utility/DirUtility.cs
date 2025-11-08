using System.Collections.Generic;
using Godot;

namespace Horizoncraft.script.Utility;

public static class DirUtility
{
    /// <summary>
    /// 加载目录下的所有文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="filelist"></param>
    public static void GetAllFiles(string path, List<string> filelist)
    {
        DirAccess dir = DirAccess.Open(path);
        if (dir == null)
        {
            GD.PrintErr($"[GetAllFiles] 无法打开{path}");
            return;
        }

        dir.ListDirBegin();
        var filename = dir.GetNext();
        while (filename != "")
        {
            if (filename == "." || filename == "..")
                continue;
            string deep_path = path + "/" + filename;
            if (dir.CurrentIsDir())
            {
                GetAllFiles(deep_path, filelist);
            }
            else
            {
                filelist.Add(deep_path);
            }

            filename = dir.GetNext();
        }

        dir.ListDirEnd();
    }
}