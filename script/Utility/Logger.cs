using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

namespace Horizoncraft.script.Utility;

public enum LogLevel
{
    None,
    Error,
    Warn,
    Info,
    Debug,
}

public static class GameLogger
{
    public static LogLevel Level = LogLevel.Info;
    public static int MaxMemoryEntries = 10000;
    public static int MaxFileLines = 50000;

    private static readonly List<string> _buffer = new();
    private static readonly object _lock = new();

    private static string _logDir = "save/logs";
    private static string _sessionFile = "";

    static GameLogger()
    {
        try
        {
            if (!DirAccess.DirExistsAbsolute(_logDir))
                DirAccess.MakeDirAbsolute(_logDir);

            var now = DateTime.Now;
            _sessionFile = $"{_logDir}/{now:yyyy-MM-dd_HH-mm-ss}.log";

            foreach (var f in DirAccess.GetFilesAt(_logDir))
            {
                if (f.EndsWith(".log"))
                {
                    var name = Path.GetFileNameWithoutExtension(f);
                    if (DateTime.TryParseExact(name, "yyyy-MM-dd_HH-mm-ss", null,
                            System.Globalization.DateTimeStyles.None, out var dt))
                    {
                        if ((now - dt).TotalDays > 7)
                            DirAccess.RemoveAbsolute(Path.Combine(_logDir, f));
                    }
                }
            }
        }
        catch
        {
        }
    }

    private static void Write(LogLevel level, string tag, string message)
    {
        if (Level < level) return;

        string line = $"[{DateTime.Now:HH:mm:ss}][{level,-5}][{tag}] {message}";

        lock (_lock)
        {
            _buffer.Add(line);
            if (_buffer.Count > MaxMemoryEntries)
                _buffer.RemoveRange(0, _buffer.Count - MaxMemoryEntries);
        }

        if (level == LogLevel.Error)
            GD.PrintErr(line);
        else
            GD.Print(line);
    }

    public static void Info(string tag, string message) => Write(LogLevel.Info, tag, message);
    public static void Warn(string tag, string message) => Write(LogLevel.Warn, tag, message);
    public static void Error(string tag, string message) => Write(LogLevel.Error, tag, message);
    public static void Debug(string tag, string message) => Write(LogLevel.Debug, tag, message);

    public static void Flush()
    {
        lock (_lock)
        {
            if (_buffer.Count == 0 || string.IsNullOrEmpty(_sessionFile)) return;

            try
            {
                var lines = _buffer.ToArray();

                string existing = "";
                if (File.Exists(_sessionFile))
                    existing = File.ReadAllText(_sessionFile);

                var allLines = existing.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
                allLines.AddRange(lines);

                if (allLines.Count > MaxFileLines)
                    allLines.RemoveRange(0, allLines.Count - MaxFileLines);

                File.WriteAllText(_sessionFile, string.Join("\n", allLines));
                _buffer.Clear();
            }
            catch
            {
            }
        }
    }
}
