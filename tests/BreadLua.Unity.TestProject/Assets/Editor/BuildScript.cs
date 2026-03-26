using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityBuilderAction
{
    public static class Builder
    {
        public static void BuildProject()
        {
            var args = Environment.GetCommandLineArgs();
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildPath = "build/" + buildTarget.ToString();

            // Parse custom build path from args
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-buildPath" && i + 1 < args.Length)
                    buildPath = args[i + 1];
                if (args[i] == "-customBuildPath" && i + 1 < args.Length)
                    buildPath = args[i + 1];
            }

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                // Use default scene if none configured
                scenes = new[] { "" };
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = buildTarget,
                options = BuildOptions.None
            };

            Debug.Log($"[BUILD] Building {buildTarget} to {buildPath}");

            var result = BuildPipeline.BuildPlayer(options);

            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"[BUILD] Build failed: {result.summary.result}");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log($"[BUILD] Build succeeded: {result.summary.totalSize} bytes");
                EditorApplication.Exit(0);
            }
        }
    }
}
