using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
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

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-buildPath" && i + 1 < args.Length)
                    buildPath = args[i + 1];
                if (args[i] == "-customBuildPath" && i + 1 < args.Length)
                    buildPath = args[i + 1];
            }

            if (buildTarget == BuildTarget.Android && !buildPath.EndsWith(".apk"))
                buildPath += ".apk";

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.Log("[BUILD] No scenes in build settings, creating empty scene");
                var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                    UnityEditor.SceneManagement.NewSceneMode.Single);
                var scenePath = "Assets/Scenes/BuildScene.unity";
                System.IO.Directory.CreateDirectory("Assets/Scenes");
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
                scenes = new[] { scenePath };
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = buildTarget,
                options = BuildOptions.None
            };

            Debug.Log($"[BUILD] Building {buildTarget} to {buildPath} with {scenes.Length} scene(s)");

            var result = BuildPipeline.BuildPlayer(options);

            if (result.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[BUILD] Build failed: {result.summary.result}");
                Debug.LogError($"[BUILD] Total errors: {result.summary.totalErrors}");
                foreach (var step in result.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                            Debug.LogError($"[BUILD] {step.name}: {msg.content}");
                    }
                }
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
