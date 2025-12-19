#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

public static class RuntimeGraphJsonDebug
{
    public static void DumpToJsonAndOpen(object runtimeGraph)
    {
        // Path where the file will be written
        string path = Path.Combine(Application.dataPath, "RuntimeCozyGraph_Debug.json");

        // Serialize (Unity-style JSON, readable)
        string json = JsonUtility.ToJson(runtimeGraph, true);

        // Write file
        File.WriteAllText(path, json);

        UnityEngine.Debug.Log($"Runtime graph JSON written to:\n{path}");

        // Open in Notepad (Windows)
        Process.Start(new ProcessStartInfo
        {
            FileName = "notepad.exe",
            Arguments = $"\"{path}\"",
            UseShellExecute = true
        });
    }
}
#endif