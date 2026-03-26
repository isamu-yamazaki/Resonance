# Resonance

## Building the game

Client build configs are assets in `Assets/Resources/ClientBuild/`. Server build configs are in `Assets/Resources/ServerBuild/`.

Output is written to `Builds/<ConfigName>/<Platform>/` from the project root.

### From the Unity Editor

Use the **Build** menu. Client builds are under **Build > Client > Windows** or **Build > Client > Mac**, server builds under **Build > Server**.

#### Client configs

| Config | Steam lobby | Relay | Orchestrator | Mode |
|---|---|---|---|---|
| DevClient | No | Remote | Local (localhost:9000) | Client-server |
| DevClientLocalRelay | No | Local (PurrLay) | Local (localhost:9000) | Client-server |
| DevClientRemoteOrchestratorTesting | No | Remote | Remote | Client-server |
| DevHost | No | Remote | — | Host |
| DevHostLocalRelay | No | Local (PurrLay) | — | Host |
| ProductionClient | Yes | Remote | Remote | Client-server |
| ProductionHost | Yes | Remote | — | Host |

- **Local relay** configs require [PurrLay](https://github.com/brendan-ch/PurrLay) running locally — see repo for instructions.
- **DevClientRemoteOrchestratorTesting** — CLI-only (not available in the Editor Build menu); connects to the production orchestrator without Steam.
- **ProductionClient / ProductionHost** — Steam lobby, release build; Mac builds run codesign & notarization.

#### Server configs

| Config | Relay |
|---|---|
| LocalRelay | Local (PurrLay) |
| Production | Remote |

Server builds target Linux64 and output to `Builds/<ConfigName>/Linux/ResonanceServer`.

### From the command line

```sh
/path/to/Unity \
  -batchmode -quit \
  -projectPath /path/to/Resonance \
  -executeMethod Resonance.BuildTools.BuildScript.BuildCLI \
  -buildMode <mode> \
  -buildConfig <config> \
  -buildTarget <platform>
```

**Arguments:**

| Argument | Required | Values |
|---|---|---|
| `-buildMode` | No (default: `Client`) | `Client`, `Server` |
| `-buildConfig` | Yes | Client: `DevClient`, `DevClientLocalRelay`, `DevClientRemoteOrchestratorTesting`, `DevHost`, `DevHostLocalRelay`, `ProductionClient`, `ProductionHost` / Server: `LocalRelay`, `Production` |
| `-buildTarget` | No (default: `Windows64`) | `Windows64`, `OSX`, `Linux64` |

### Production Mac builds (codesign & notarization)

Set the following environment variables before running a Production Mac build:

| Variable | Description |
|---|---|
| `SIGNING_IDENTITY` | Developer ID certificate name (omit to ad-hoc sign with `-`) |
| `APPLE_ID` | Apple ID email for notarization |
| `APPLE_APP_PASSWORD` | App-specific password |
| `APPLE_TEAM_ID` | Apple Developer Team ID |

If credentials are missing, the build will still succeed but notarization will be skipped.
