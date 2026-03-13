using System.Collections.Generic;
using System.IO;
using Godot;
using Horizoncraft.script.Net;
using Horizoncraft.script.WorldControl.Tool;
using FileAccess = Godot.FileAccess;

namespace Horizoncraft.script.Utility;

public static class DirUtility
{
    /// <summary>
    /// 获目录下的全部文件（递归目录）
    /// 支持 res:// 目录
    /// </summary>
    /// <param name="path"></param>
    /// <param name="filelist"></param>
    public static void GetFiles(string path,string extenstion, List<string> filelist)
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
            {
                filename = dir.GetNext();
                continue;
            }
            string deep_path = Path.Combine(path, filename);
            if (dir.CurrentIsDir())
            {
                GetFiles(deep_path, extenstion,filelist);
            }
            else
            {
                if(Path.GetExtension(deep_path)==extenstion)
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