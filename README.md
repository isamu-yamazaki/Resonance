# Resonance

## LFS setup

To bypass the [bandwidth limits](https://docs.github.com/en/billing/concepts/product-billing/git-lfs), this project uses a custom LFS server instead of GitHub's.

Cloning is public and requires no authentication. When pushing new files, you should be asked by your LFS client to enter a username and password.

- Username: your GitHub username
- Password: a [GitHub personal access token (PAT)](https://github.com/settings/personal-access-tokens) with read-write permissions

If `git lfs push` (or `git push` with new LFS files) fails with an authentication error instead of prompting you for a username and password:

1. **Check your credential helper is set:**
   ```sh
   git config credential.helper
   ```
   If empty, set one:
   - macOS: `git config --global credential.helper osxkeychain`
   - Windows: `git config --global credential.helper manager`
   - Linux: `git config --global credential.helper store`

2. **Clear stale credentials:**
   - macOS: open Keychain Access, search for `lfs.bchen.dev`, and delete the entry
   - Windows: open Credential Manager > Windows Credentials, find `git:https://lfs.bchen.dev`, and remove it
   - Linux (`store` helper): edit `~/.git-credentials` and remove the line containing `lfs.bchen.dev`

3. **Test that the LFS server is reachable:**
   ```sh
   git lfs env
   ```
   Verify the `Endpoint` URL matches `https://lfs.bchen.dev/isamu-yamazaki/resonance`. If it doesn't, check that `.lfsconfig` is present and not overridden by a local git config.

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
  -buildTarget <platform> \
  -buildPlatform <platform>
```

**Arguments:**

| Argument | Required | Values |
|---|---|---|
| `-buildMode` | No (default: `Client`) | `Client`, `Server` |
| `-buildConfig` | Yes | Client: `DevClient`, `DevClientLocalRelay`, `DevClientRemoteOrchestratorTesting`, `DevHost`, `DevHostLocalRelay`, `ProductionClient`, `ProductionHost` / Server: `LocalRelay`, `Production` |
| `-buildTarget` | No (default: `win64`) | Unity's built-in platform switch: `win64`, `osxuniversal`, `linux64` |
| `-buildPlatform` | No (default: `win64`) | `win64`, `osxuniversal`, `linux64` |

### Production Mac builds (codesign & notarization)

Set the following environment variables before running a Production Mac build:

| Variable | Description |
|---|---|
| `SIGNING_IDENTITY` | Developer ID certificate name (omit to ad-hoc sign with `-`) |
| `APPLE_ID` | Apple ID email for notarization |
| `APPLE_APP_PASSWORD` | App-specific password |
| `APPLE_TEAM_ID` | Apple Developer Team ID |

If credentials are missing, the build will still succeed but notarization will be skipped.
