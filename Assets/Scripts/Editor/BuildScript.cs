#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Resonance.BuildTools
{
    public static class BuildScript
    {

        #region Editor menu items

        [MenuItem("Build/Windows/DevLocal")]
        public static void BuildDevLocalWindows() => Build(LoadConfig("DevLocal"), BuildTarget.StandaloneWindows64);

        [MenuItem("Build/Windows/Dev")]
        public static void BuildDevWindows() => Build(LoadConfig("Dev"), BuildTarget.StandaloneWindows64);

        [MenuItem("Build/Windows/Production")]
        public static void BuildProductionWindows() => Build(LoadConfig("Production"), BuildTarget.StandaloneWindows64);

        [MenuItem("Build/Mac/DevLocal")]
        public static void BuildDevLocalMac() => Build(LoadConfig("DevLocal"), BuildTarget.StandaloneOSX);

        [MenuItem("Build/Mac/Dev")]
        public static void BuildDevMac() => Build(LoadConfig("Dev"), BuildTarget.StandaloneOSX);

        [MenuItem("Build/Mac/Production")]
        public static void BuildProductionMac() => Build(LoadConfig("Production"), BuildTarget.StandaloneOSX);

        #endregion


        #region CLI entry point
        /// <summary>
        /// Invoked via: /path/to/Unity -executeMethod BuildScript.BuildCLI -buildConfig AppConfig_Staging -buildTarget Windows64
        /// Supported -buildTarget values: Windows64, OSX
        /// </summary>
        public static void BuildCLI()
        {
            string configName = ReadArg("-buildConfig")
                ?? throw new System.Exception("Missing -buildConfig argument. Usage: -buildConfig <AssetName>");

            string targetName = ReadArg("-buildTarget") ?? "Windows64";
            BuildTarget target = targetName switch
            {
                "Windows64" => BuildTarget.StandaloneWindows64,
                "OSX" => BuildTarget.StandaloneOSX,
                _ => throw new System.Exception($"Unknown -buildTarget '{targetName}'. Supported: Windows64, OSX"),
            };

            Build(LoadConfig(configName), target);
        }
        #endregion

        #region Internal
        static BuildConfig LoadConfig(string assetName)
        {
            string path = $"Assets/Resources/Build/{assetName}.asset";
            var config = AssetDatabase.LoadAssetAtPath<BuildConfig>(path);
            if (config == null)
            {
                throw new System.Exception($"Could not load AppConfig at '{path}'. Check the asset name.");
            }
            return config;
        }

        static void Build(BuildConfig config, BuildTarget target)
        {
            InjectConfigIntoScene<LobbySceneConfigurator>(
                "Assets/Scenes/Lobby/LobbyScene.unity", config);
            InjectConfigIntoScene<PurrTransportConfigurator>(
                "Assets/Scenes/Transitions/GameBootstrapScene.unity", config);

            bool isDev = !config.enableSteamLobby && !config.useProductionRelay;
            string ext = target == BuildTarget.StandaloneWindows64 ? ".exe" : ".app";
            string targetFolder = target == BuildTarget.StandaloneWindows64 ? "Windows" : "Mac";

            var options = new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path),
                locationPathName = $"Builds/{config.name}/{targetFolder}/Resonance{ext}",
                target = target,
                options = isDev ? BuildOptions.Development : BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.Exception($"Build failed for config '{config.name}' target '{target}'");
            }

            if (config.isProduction)
            {
                PostBuild(report.summary.outputPath, target);
            }
        }

        static void PostBuild(string outputPath, BuildTarget target)
        {
            CopySteamAppId(outputPath, target);

            if (target == BuildTarget.StandaloneOSX)
            {
                CodeSignAndNotarizeMac(outputPath);
            }
        }

        static void CopySteamAppId(string outputPath, BuildTarget target)
        {
            string src = Path.GetFullPath(Path.Combine(Application.dataPath, "../steam_appid.txt"));
            if (!File.Exists(src))
            {
                Debug.LogWarning("[BuildScript] steam_appid.txt not found at project root — skipping.");
                return;
            }

            string dst = target == BuildTarget.StandaloneWindows64
                ? Path.Combine(Path.GetDirectoryName(outputPath), "steam_appid.txt")
                : Path.Combine(outputPath, "Contents/MacOS/steam_appid.txt");

            File.Copy(src, dst, overwrite: true);
            Debug.Log($"[BuildScript] Copied steam_appid.txt → {dst}");
        }

        static void CodeSignAndNotarizeMac(string appPath)
        {
#if UNITY_EDITOR_OSX
            string identity = System.Environment.GetEnvironmentVariable("SIGNING_IDENTITY") ?? "-";
            string appleId = System.Environment.GetEnvironmentVariable("APPLE_ID");
            string appPassword = System.Environment.GetEnvironmentVariable("APPLE_APP_PASSWORD");
            string teamId = System.Environment.GetEnvironmentVariable("APPLE_TEAM_ID");
            string entitlements = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../Mac.entitlements"));

            Debug.Log("[BuildScript] Codesigning...");
            RunShell($"codesign --deep --force --sign \"{identity}\" " +
                     $"--entitlements \"{entitlements}\" \"{appPath}\"");

            bool canNotarize = identity != "-"
                && !string.IsNullOrEmpty(appleId)
                && !string.IsNullOrEmpty(appPassword)
                && !string.IsNullOrEmpty(teamId);

            if (!canNotarize)
            {
                Debug.LogWarning("[BuildScript] Apple credentials not set — skipping notarization.");
                return;
            }

            string zipPath = appPath + "_notarize.zip";
            RunShell($"ditto -c -k --keepParent \"{appPath}\" \"{zipPath}\"");

            Debug.Log("[BuildScript] Submitting for notarization (may take a few minutes)...");
            int result = RunShell(
                $"xcrun notarytool submit \"{zipPath}\" " +
                $"--apple-id \"{appleId}\" --password \"{appPassword}\" " +
                $"--team-id \"{teamId}\" --wait");

            File.Delete(zipPath);

            if (result != 0)
            {
                throw new System.Exception($"notarytool failed (exit {result})");
            }

            Debug.Log("[BuildScript] Stapling notarization ticket...");
            RunShell($"xcrun stapler staple \"{appPath}\"");
            Debug.Log("[BuildScript] Notarization complete.");
#else
        Debug.LogWarning("[BuildScript] Mac signing requires a macOS editor/CI — skipped.");
#endif
        }

#if UNITY_EDITOR_OSX
        static int RunShell(string command)
        {
            using var proc = new System.Diagnostics.Process();
            proc.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/sh",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            proc.StartInfo.ArgumentList.Add("-c");
            proc.StartInfo.ArgumentList.Add(command);
            proc.Start();
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            if (!string.IsNullOrWhiteSpace(stdout)) { Debug.Log(stdout); }
            if (!string.IsNullOrWhiteSpace(stderr)) { Debug.LogWarning(stderr); }
            return proc.ExitCode;
        }
#endif

        static void InjectConfigIntoScene<T>(string scenePath, BuildConfig config) where T : MonoBehaviour
        {
            bool wasAlreadyLoaded = false;
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path == scenePath)
                {
                    wasAlreadyLoaded = true;
                    break;
                }
            }

            var scene = wasAlreadyLoaded
                ? EditorSceneManager.GetSceneByPath(scenePath)
                : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            var configurator = Object.FindFirstObjectByType<T>();
            if (configurator == null)
            {
                Debug.LogWarning($"[BuildScript] {typeof(T).Name} not found in {scenePath}. Config not injected.");
                if (!wasAlreadyLoaded)
                    EditorSceneManager.CloseScene(scene, true);
                return;
            }

            var so = new SerializedObject(configurator);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);

            if (!wasAlreadyLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }

        static string ReadArg(string flag)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == flag)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
        #endregion
    }
}

#endif
