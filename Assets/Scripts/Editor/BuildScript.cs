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

        [MenuItem("Build/Client/Windows/DevClient")]
        public static void BuildDevClientWindows() => Build(LoadConfig("DevClient"), BuildTarget.StandaloneWindows64);

        [MenuItem("Build/Client/Windows/DevClientLocalRelay")]
        public static void BuildDevClientLocalRelayWindows() => Build(LoadConfig("DevClientLocalRelay"), BuildTarget.StandaloneWindows64);

        [MenuItem("Build/Client/Windows/DevHost")]
        public static void BuildDevHostWindows() => Build(LoadConfig("DevHost"), BuildTarget.StandaloneWindows64);

        [MenuItem("Build/Client/Windows/DevHostLocalRelay")]
        public static void BuildDevHostLocalRelayWindows() => Build(LoadConfig("DevHostLocalRelay"), BuildTarget.StandaloneWindows64);

        [MenuItem("Build/Client/Windows/ProductionClient")]
        public static void BuildProductionClientWindows() => Build(LoadConfig("ProductionClient"), BuildTarget.StandaloneWindows64);

        [MenuItem("Build/Client/Windows/ProductionHost")]
        public static void BuildProductionHostWindows() => Build(LoadConfig("ProductionHost"), BuildTarget.StandaloneWindows64);

        [MenuItem("Build/Client/Mac/DevClient")]
        public static void BuildDevClientMac() => Build(LoadConfig("DevClient"), BuildTarget.StandaloneOSX);

        [MenuItem("Build/Client/Mac/DevClientLocalRelay")]
        public static void BuildDevClientLocalRelayMac() => Build(LoadConfig("DevClientLocalRelay"), BuildTarget.StandaloneOSX);

        [MenuItem("Build/Client/Mac/DevHost")]
        public static void BuildDevHostMac() => Build(LoadConfig("DevHost"), BuildTarget.StandaloneOSX);

        [MenuItem("Build/Client/Mac/DevHostLocalRelay")]
        public static void BuildDevHostLocalRelayMac() => Build(LoadConfig("DevHostLocalRelay"), BuildTarget.StandaloneOSX);

        [MenuItem("Build/Client/Mac/ProductionClient")]
        public static void BuildProductionClientMac() => Build(LoadConfig("ProductionClient"), BuildTarget.StandaloneOSX);

        [MenuItem("Build/Client/Mac/ProductionHost")]
        public static void BuildProductionHostMac() => Build(LoadConfig("ProductionHost"), BuildTarget.StandaloneOSX);

        [MenuItem("Build/Server/Linux/LocalRelay")]
        public static void BuildServerLocalRelayLinux() => BuildServer(LoadServerConfig("LocalRelay"), BuildTarget.StandaloneLinux64);

        [MenuItem("Build/Server/Linux/Production")]
        public static void BuildServerProductionLinux() => BuildServer(LoadServerConfig("Production"), BuildTarget.StandaloneLinux64);

        #endregion


        #region CLI entry point
        /// <summary>
        /// Invoked via: /path/to/Unity -executeMethod BuildScript.BuildCLI -buildMode Client|Server -buildConfig &lt;AssetName&gt; -buildTarget Windows64|OSX|Linux64
        /// Supported -buildMode values: Client (default), Server
        /// Supported -buildTarget values: Windows64 (default), OSX, Linux64
        /// </summary>
        public static void BuildCLI()
        {
            string configName = ReadArg("-buildConfig")
                ?? throw new System.Exception("Missing -buildConfig argument. Usage: -buildConfig <AssetName>");

            string modeName = ReadArg("-buildMode") ?? "Client";
            string targetName = ReadArg("-buildTarget") ?? "Windows64";
            BuildTarget target = targetName switch
            {
                "Windows64" => BuildTarget.StandaloneWindows64,
                "OSX" => BuildTarget.StandaloneOSX,
                "Linux64" => BuildTarget.StandaloneLinux64,
                _ => throw new System.Exception($"Unknown -buildTarget '{targetName}'. Supported: Windows64, OSX, Linux64"),
            };

            if (modeName == "Server")
            {
                BuildServer(LoadServerConfig(configName), target);
            }
            else
            {
                Build(LoadConfig(configName), target);
            }
        }
        #endregion

        #region Internal
        static ClientBuildConfig LoadConfig(string assetName)
        {
            string path = $"Assets/Resources/ClientBuild/{assetName}.asset";
            var config = AssetDatabase.LoadAssetAtPath<ClientBuildConfig>(path);
            if (config == null)
            {
                throw new System.Exception($"Could not load ClientBuildConfig at '{path}'. Check the asset name.");
            }
            return config;
        }

        static ServerBuildConfig LoadServerConfig(string assetName)
        {
            string path = $"Assets/Resources/ServerBuild/{assetName}.asset";
            var config = AssetDatabase.LoadAssetAtPath<ServerBuildConfig>(path);
            if (config == null)
            {
                throw new System.Exception($"Could not load ServerBuildConfig at '{path}'. Check the asset name.");
            }
            return config;
        }

        static void BuildServer(ServerBuildConfig config, BuildTarget target)
        {
            bool isDev = !config.useProductionRelay;
            string targetFolder = target == BuildTarget.StandaloneLinux64 ? "Linux" : "Windows";

            var options = new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path),
                locationPathName = $"Builds/{config.name}/{targetFolder}/ResonanceServer",
                target = target,
                options = isDev ? BuildOptions.Development : BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.Exception($"Server build failed for config '{config.name}' target '{target}'");
            }
        }

        static void Build(ClientBuildConfig config, BuildTarget target)
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
                Path.Combine(Application.dataPath, "../entitlements.plist"));

            Debug.Log("[BuildScript] Codesigning...");
            RunShell($"codesign --options runtime --timestamp --deep --force --sign \"{identity}\" " +
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

        static void InjectConfigIntoScene<T>(string scenePath, ClientBuildConfig config) where T : MonoBehaviour
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
