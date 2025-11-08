using System.Collections.Generic;
using Godot;
using Horizoncraft.script.Net;
using Horizoncraft.script.WorldControl.Tool;

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

    public static WorldProfile GetWorldProfile(string worldName)
    {
        if (FileAccess.FileExists($"save/{worldName}/data.db"))
        {
            using (var conn = SqliteTool.InitSqlite(worldName))
            {
                var file = conn.GetWorldProfileByteData("WorldProfile");
                return file;
            }
        }

        return null;
    }
}