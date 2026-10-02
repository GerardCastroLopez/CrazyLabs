using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CrazyLabs.EditorTools
{
    /// <summary>
    /// Development convenience: when <c>Temp/sledrun-build.request</c> exists, the next script reload
    /// builds the prototype scene and writes everything it logged to <c>Logs/sledrun-build.txt</c>.
    /// Lets the scene be rebuilt and verified from outside the editor (touch the request file, then edit this script to force a reload).
    /// </summary>
    [InitializeOnLoad]
    static class AutoBuildHook
    {
        const string RequestFile = "Temp/sledrun-build.request";
        const string ResultFile = "Logs/sledrun-build.txt";

        static readonly StringBuilder Output = new StringBuilder();

        static AutoBuildHook()
        {
            if (!File.Exists(RequestFile)) return;
            File.Delete(RequestFile);
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            Output.Clear();
            Application.logMessageReceived += Capture;
            try
            {
                Output.AppendLine("== build started ==");
                bool executed = EditorApplication.ExecuteMenuItem("Tools/Sled Run/Build Prototype Scene");
                Output.AppendLine(executed ? "== build finished ==" : "== menu item not found ==");
            }
            finally
            {
                Application.logMessageReceived -= Capture;
                File.WriteAllText(ResultFile, Output.ToString());
            }
        }

        static void Capture(string message, string stackTrace, LogType type)
        {
            Output.AppendLine($"[{type}] {message}");
            if (type == LogType.Exception || type == LogType.Error) Output.AppendLine(stackTrace);
        }
    }
}

