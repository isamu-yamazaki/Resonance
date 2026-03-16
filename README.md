# Resonance

## Building the game

Build configs are assets located at `Assets/Resources/Build/`. Three configs exist: `DevLocal`, `Dev`, and `Production`.

Output is written to `Builds/<ConfigName>/<Platform>/` from the project root.

### From the Unity Editor

Use the **Build** menu to choose a platform and one of the options:

- **DevLocal** — local relay, dev build, dummy lobby
    - Requires [PurrLay](https://github.com/brendan-ch/PurrLay) to be running on your computer, see instructions in repo
- **Dev** — remote relay, dev build, dummy lobby
- **Production** — remote relay + Steam lobby, release build; runs codesign & notarization for Mac

### From the command line

```sh
/path/to/Unity \
  -batchmode -quit \
  -projectPath /path/to/Resonance \
  -executeMethod Resonance.BuildTools.BuildScript.BuildCLI \
  -buildConfig <config> \
  -buildTarget <platform>
```

**Arguments:**

| Argument | Required | Values |
|---|---|---|
| `-buildConfig` | Yes | `DevLocal`, `Dev`, `Production` |
| `-buildTarget` | No (default: `Windows64`) | `Windows64`, `OSX` |

### Production Mac builds (codesign & notarization)

Set the following environment variables before running a Production Mac build:

| Variable | Description |
|---|---|
| `SIGNING_IDENTITY` | Developer ID certificate name (omit to ad-hoc sign with `-`) |
| `APPLE_ID` | Apple ID email for notarization |
| `APPLE_APP_PASSWORD` | App-specific password |
| `APPLE_TEAM_ID` | Apple Developer Team ID |

If credentials are missing, the build will still succeed but notarization will be skipped.
