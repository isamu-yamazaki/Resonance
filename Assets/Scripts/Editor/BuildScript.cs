#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
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
        /// Invoked via: /path/to/Unity -executeMethod BuildScript.BuildCLI -buildMode Client|Server -buildConfig &lt;AssetName&gt; -buildPlatform win64|osxuniversal|linux64
        /// Supported -buildMode values: Client (default), Server
        /// Supported -buildPlatform values: win64 (default), osxuniversal, linux64
        /// </summary>
        public static void BuildCLI()
        {
            string configName = ReadArg("-buildConfig")
                ?? throw new System.Exception("Missing -buildConfig argument. Usage: -buildConfig <AssetName>");

            string modeName = ReadArg("-buildMode") ?? "Client";
            string targetName = ReadArg("-buildPlatform") ?? "win64";
            BuildTarget target = targetName switch
            {
                "win64" => BuildTarget.StandaloneWindows64,
                "osxuniversal" => BuildTarget.StandaloneOSX,
                "linux64" => BuildTarget.StandaloneLinux64,
                _ => throw new System.Exception($"Unknown -buildPlatform '{targetName}'. Supported: win64, osxuniversal, linux64"),
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

        const string ClientBuildConfigPath = "Assets/Resources/ClientBuild/";
        const string ServerBuildConfigPath = "Assets/Resources/ServerBuild/";
        const string LinuxServerProfilePath = "Assets/Settings/Build Profiles/Linux Server.asset";

        static string[] SharedScenes =>
            System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path);

        static string[] ServerScenes
        {
            get
            {
                var profile = LoadAsset<BuildProfile>(LinuxServerProfilePath);
                return profile.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            }
        }

        static T LoadAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new System.Exception($"Could not load {typeof(T).Name} at '{path}'.");
            }
            return asset;
        }

        static ClientBuildConfig LoadConfig(string name) =>
            LoadAsset<ClientBuildConfig>($"{ClientBuildConfigPath}{name}.asset");

        static ServerBuildConfig LoadServerConfig(string name) =>
            LoadAsset<ServerBuildConfig>($"{ServerBuildConfigPath}{name}.asset");

        static void LogScenes(string[] scenes)
        {
            Debug.Log($"[BuildScript] Building with {scenes.Length} scene(s):\n" +
                      string.Join("\n", scenes.Select((s, i) => $"  [{i}] {s}")));
        }

        static void VerifyBuild(BuildReport report)
        {
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new System.Exception(
                    $"Build failed for '{report.summary.outputPath}' (result: {report.summary.result})");
            }
        }

        static void BuildServer(ServerBuildConfig config, BuildTarget target)
        {
            InjectConfigIntoScene<ServerBuildConfigReceiver, ServerBuildConfig>(
                "Assets/Scenes/Transitions/ServerStartScene.unity", config);

            bool isDev = !config.useProductionRelay;
            string targetFolder = target == BuildTarget.StandaloneLinux64 ? "Linux" : "Windows";

            string[] scenes = ServerScenes;
            LogScenes(scenes);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = $"Builds/{config.name}/{targetFolder}/ResonanceServer.x86_64",
                target = target,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = isDev ? BuildOptions.Development : BuildOptions.None,
            };

            VerifyBuild(BuildPipeline.BuildPlayer(options));
        }

        static void Build(ClientBuildConfig config, BuildTarget target)
        {
            InjectConfigIntoScene<ClientBuildConfigReceiver, ClientBuildConfig>(
                "Assets/Scenes/Lobby/LobbyScene.unity", config);

            bool isDev = !config.enableSteamLobby && !config.useProductionRelay;
            string ext = target == BuildTarget.StandaloneWindows64 ? ".exe" : ".app";
            string targetFolder = target == BuildTarget.StandaloneWindows64 ? "Windows" : "Mac";

            string[] scenes = SharedScenes;
            LogScenes(scenes);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = $"Builds/{config.name}/{targetFolder}/Resonance{ext}",
                target = target,
                options = isDev ? BuildOptions.Development : BuildOptions.None,
            };

            VerifyBuild(BuildPipeline.BuildPlayer(options));

            if (config.isProduction)
            {
                PostBuild(options.locationPathName, target);
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

        static void InjectConfigIntoScene<TConfigurator, TConfig>(string scenePath, TConfig config)
            where TConfigurator : MonoBehaviour
            where TConfig : ScriptableObject
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

            var configurator = Object.FindFirstObjectByType<TConfigurator>();
            if (configurator == null)
            {
                Debug.LogWarning($"[BuildScript] {typeof(TConfigurator).Name} not found in {scenePath}. Config not injected.");
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
