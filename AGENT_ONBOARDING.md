# AI Agent Onboarding — FTK2 Voice Acting Mod

This document captures hard-won knowledge for any AI agent working on this codebase. Read this before making changes.

## Quick Reference

| Item | Value |
|---|---|
| Framework | BepInEx 5 (Mono backend); source project gets BepInEx from NuGet (`5.*`) |
| Plugin target | .NET Standard 2.1 (`src/FTK2VoiceActing/FTK2VoiceActing.csproj`) |
| Test target | .NET 8.0 (`tests/FTK2VoiceActing.Tests/FTK2VoiceActing.Tests.csproj`) |
| Unity | 2022.3.41 (observed from game install) |
| Game | For the King 2 (`For The King II.exe`) |
| Solution | `FTK2VoiceActing.sln` |
| Plugin GUID | `dev.ftk2.voiceacting` |
| Decompiled source | External, at user's discretion (originally `D:\FTK2_Src`) |
| Build | `dotnet build` (first build requires NuGet restore via `nuget.config`) |
| Test | `dotnet test` — 111 NUnit tests, all must pass |
| Deploy | Copy `src/.../bin/Debug/netstandard2.1/FTK2VoiceActing.dll` to game's `BepInEx\plugins\FTK2VoiceActing\` |

## Build Prerequisites

Building the solution has two dependency layers:

1. **NuGet packages** — restored via feeds in `nuget.config` (`nuget.org` + `https://nuget.bepinex.dev/v3/index.json`). The source project gets BepInEx this way.
2. **Manually copied DLLs** in `lib/` — Unity engine and game assemblies. See `lib/README.md` for the full list and a PowerShell copy command.

The source project references game DLLs as `Private=false` (not copied to output). The test project references some DLLs with `Private=true` (copied for test runner).

`Directory.Build.props` sets `LangVersion=latest` and `Nullable=disable` globally. The source project has `InternalsVisibleTo("FTK2VoiceActing.Tests")` so tests can access internal APIs.

## Architecture

```
Plugin.cs (BepInEx entry)
  ├── VoiceConfig.cs          — ConfigEntry wrappers (Enabled, Volume, DebugLogging)
  │                              Note: DebugLogging only gates scan/index diagnostics in VoiceManager;
  │                              other LogDebug calls (patch tracing, playback events) are unconditional
  ├── VoiceManager.cs         — File index, async audio loading, playback
  │     └── AudioPlaybackHandle.cs — Unity AudioSource lifecycle + generation counter
  ├── DialoguePatches.cs      — Harmony patches: RenderEmitter, RenderSay, Deinitialize
  └── LoadingScreenPatches.cs — Harmony patches: Initialize (postfix), Hide (prefix)
```

### Dependency injection for testing

- `IVoicePlayback` interface decouples patches from `VoiceManager`
- `FakeVoicePlayback` is the test double (tracks calls, simulates clips)
- `AudioPlaybackHandle` uses protected virtual methods for all Unity API calls
- `TestableAudioPlaybackHandle` overrides them to avoid Unity native ECall crashes in NUnit
- `RecreatingTestHandle` extends `TestableAudioPlaybackHandle` to simulate scene-transition destruction

## Critical Game Behavior (Learned the Hard Way)

### 1. SAY values may arrive pre-translated at runtime

**Decompiled source** shows `GameplayDirectorBase.ParseDialogueAction()` passing raw JSON strings to `RenderSay`, and `DialogueViewHelper.RenderSay()` calling `Lang.__dt(pValue)` internally when `pDoTranslate=true`. This suggests the Harmony prefix should see raw dialogue keys.

**Observed runtime behavior** contradicts this: BepInEx logs confirmed that `RenderSayPrefix` receives `pValue='My, we haven't seen many adventurers in these parts for a spell.'` (translated English text), with `pDoTranslate=True`. The game appears to pre-translate SAY values somewhere upstream of `RenderSay` at runtime, possibly through a different code path than the decompiled source suggests.

**EMITTER values are NOT translated** — `pValue` is the raw NPC ID like `NPC_BARMAID` (confirmed both in source and at runtime). Note: `REFLECTION_*` emitters are converted to display names with `pDoTranslate=false` (`GameplayDirectorBase.cs:1982-1988`); the mod intentionally ignores these.

The mod handles both scenarios with a two-step lookup in `RenderSayPrefix`:
1. Try direct key match (`HasVoiceClip(emitter, pValue)`) — works if pValue is a raw dialogue key
2. Reverse-lookup via `VoiceManager.FindKeyByTranslatedText()` which calls `Lang.__dt(key)` on each indexed key for the NPC and compares the result to the received text (linear scan, case-insensitive, no normalization beyond `OrdinalIgnoreCase`)

Note: `RenderSayPrefix` does **not** branch on `pDoTranslate` — it always tries both paths regardless. This makes it resilient to whichever value the game sends.

**Important:** If you investigate this further, check the runtime logs — don't trust the decompiled source alone for the SAY path. The two-step lookup exists precisely because both paths are possible.

### 2. Unity destroys DontDestroyOnLoad objects during scene transitions

Unity calls `OnDestroy` on `MonoBehaviour` instances attached to `DontDestroyOnLoad` GameObjects during the initial scene load. It also destroys standalone `GameObject`s created during `Awake()`.

**Consequences and fixes:**
- `Plugin.OnDestroy()` has a `_quitting` guard — only cleans up when `OnApplicationQuit()` has fired
- `AudioPlaybackHandle.Play()` calls `EnsureCreated()` which detects destroyed Unity objects (via Unity's overloaded `== null`) and recreates them automatically
- `StopUnityPlayback()` and `PlayUnityClip()` have `if (_source == null) return;` guards
- The `_created` flag tracks logical state; `IsUnityObjectDestroyed()` checks actual native object liveness

### 3. RenderSay must stop the previous clip

When the player advances dialogue, `RenderSayPrefix` fires for the next line. If no voice file exists for that line, the previous clip must still be stopped. The stop call is near the top of `RenderSayPrefix`, after the null/empty `pValue` early return but before any key matching.

**Edge case:** If `pValue` is null or empty, the method returns early without stopping. This is acceptable since the game wouldn't display meaningful dialogue in that case.

### 4. CurrentEmitter can go stale

`CurrentEmitter` is only updated when `RenderEmitter` is called with `pDoTranslate=true` (raw NPC IDs). `REFLECTION_*` emitters arrive with `pDoTranslate=false` and are ignored, leaving the previous emitter cached. This means if a reflection-type emitter speaks, the mod would attempt to match against the previous NPC's voice files. This is an accepted limitation — reflection emitters are rare and don't have dedicated voice files.

### 5. LoadAndPlayClip must not check IsReady

`Play()` auto-recreates Unity objects via `EnsureCreated()`. An `IsReady` check before the async load would incorrectly bail out because `IsReady` detects destroyed objects. The guard was removed — let `Play()` handle it.

## Generation Counter Pattern

`AudioPlaybackHandle` uses a generation counter to handle async safety:
1. `Stop()` increments `_generation` and returns the new value
2. `PlayVoiceClip()` captures `_handle.Generation` before starting the async load
3. The async callback checks `IsCurrentGeneration(capturedGen)` — if false, a newer clip was requested while loading, so the stale clip is destroyed
4. `Play(clip, volume, requestGeneration)` also verifies the generation matches

This prevents race conditions when dialogue advances faster than audio loads.

## Unity Testing Constraints

- Unity's `AudioClip`/`AudioSource` constructors invoke native code → `SecurityException` in .NET 8 test runner
- Use `RuntimeHelpers.GetUninitializedObject(typeof(AudioClip))` to create a non-null `AudioClip` without invoking the constructor — only for passing null checks, never access its members
- `ReferenceEquals(clip, null)` is used instead of `clip == null` to avoid Unity's overloaded `==` operator which invokes native code
- All 6 Unity-facing methods in `AudioPlaybackHandle` are `protected virtual` for test overriding: `CreateUnityObjects`, `DestroyUnityObjects`, `StopUnityPlayback`, `PlayUnityClip`, `GetIsPlaying`, `IsUnityObjectDestroyed`

## External Assembly References

The source project gets BepInEx from **NuGet** (via `nuget.config`). All other game/Unity assemblies come from the `lib/` folder, populated manually from the game installation. See `lib/README.md` for the full list.

Source project references (`Private=false`, not copied to output):
- `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.AudioModule.dll`, `UnityEngine.UIElementsModule.dll`, `UnityEngine.UnityWebRequestAudioModule.dll`, `UnityEngine.UnityWebRequestModule.dll` — from game's `Managed/`
- `FTK2.dll` — from game's `Managed/` (contains `DialogueViewHelper`, `LoadingScreenViewHelper`, `Lang`)

Test project references (`Private=true`, copied for test runner):
- `BepInEx.dll`, `0Harmony.dll` — from `lib/` (BepInEx core)
- Select Unity DLLs — from `lib/`

## File Layout at Runtime

```
<game>/BepInEx/plugins/FTK2VoiceActing/
├── FTK2VoiceActing.dll
└── VoiceAssets/
    ├── NARRATOR/
    │   └── STORY_1_1_INTRO.ogg
    ├── NPC_BARMAID/
    │   └── STORY_1_1_VISIT_PRAN_PRE_DIALOG_1.ogg
    └── ...
```

The plugin auto-discovers its own directory via `Path.GetDirectoryName(Info.Location)`. No hardcoded paths.

## Voice Asset Lookup Rules

- Only files in the exact layout `VoiceAssets\<NPC_ID>\<DIALOGUE_KEY>.ogg|.wav` are indexed
- Files placed directly under `VoiceAssets\` root are ignored
- Nested subfolders under an NPC folder are also ignored
- Lookup is **case-insensitive** for both NPC IDs and dialogue keys (`StringTupleComparer.OrdinalIgnoreCase`)
- When both `.ogg` and `.wav` exist for the same key, `.ogg` wins (priority ordering)
- Missing voice files are logged once per `(npcId, dialogueKey)` pair via `_warnedMissingKeys` to avoid log spam
- `FindKeyByTranslatedText()` only searches keys already present in `_voiceFileIndex` — it is not a global dialogue search

## Runtime Behavior Notes

- **All voice playback is single-channel and mutually exclusive.** Narrator and dialogue share one `VoiceManager` instance (and one `AudioPlaybackHandle`). Starting any new clip — dialogue, narrator, or loading screen — stops whatever is currently playing.
- When `Enabled=false`, `PlayVoiceClip()` still calls `StopCurrentClip()` to immediately silence current playback and invalidate any in-flight async load that started while enabled
- Async load callbacks always dispose their `UnityWebRequest` on every code path
- Stale-generation clips (where dialogue advanced during loading) are explicitly destroyed to avoid leaking orphan `AudioClip` instances
- `RenderSayPrefix` calls `StopCurrentClip()`, then `PlayVoiceClip()` calls it again if a match is found — the second stop increments the generation counter again but is functionally harmless since no clip is playing between the two calls

## Common Pitfalls

1. **Don't add an `IsReady` guard before async audio loading** — `Play()` handles recreation
2. **Don't use `clip == null`** in test code — use `ReferenceEquals(clip, null)` to avoid Unity native calls
3. **Don't clean up in `OnDestroy` without a quit guard** — Unity fires it during scene transitions
4. **Don't assume `_created` means objects are alive** — always check via `IsUnityObjectDestroyed()`
5. **Don't forget to stop audio on SAY** — `RenderSayPrefix` must stop the previous clip before key matching (after null check)
6. **Don't commit files to `VoiceAssets_Template/`** — `.gitignore` excludes `.ogg`, `.wav`, and generated CSVs
7. **Don't add direct project references to the game source** — game types come from `lib/*.dll`; BepInEx comes from NuGet

## Useful Decompiled Source Files

If you have access to the decompiled game source:

### Game API (stable references)

| File | What it tells you |
|---|---|
| `GameplayDirectorBase.cs` (line ~1965) | `ParseDialogueAction` dispatcher — how SAY/EMITTER reach `RenderSay`/`RenderEmitter` |
| `DialogueViewHelper.cs` | `RenderSay`, `RenderEmitter`, `Deinitialize` signatures |
| `LoadingScreenViewHelper.cs` | `Initialize(string, VisualElement)`, `Hide(Action)` signatures |
| `Lang.cs` | `__t()` (translate), `__dt()` (dialogue translate) — used for reverse lookup |

### Game content (counts may change with updates)

| Path | Approximate count |
|---|---|
| `StreamingAssets/Assets/Configs/JSON~/Dialogues/` | ~652 dialogue JSON files |
| `DialogueLangs/en.json` | ~1,337 dialogue translation keys |
| `Langs/en.json` | ~9,630 general translation keys |

## Build / Test / Deploy Cycle

```powershell
dotnet build --no-restore                    # Build (< 2 seconds)
dotnet test --no-build                       # Test (< 1 second, 111 tests)

# Deploy:
Copy-Item "src\FTK2VoiceActing\bin\Debug\netstandard2.1\FTK2VoiceActing.dll" `
    "<game>\BepInEx\plugins\FTK2VoiceActing\" -Force

# Check logs after running game:
Select-String "FTK2 Voice Acting" "<game>\BepInEx\LogOutput.log"
```

## Test Conventions

- Tests use `InternalsVisibleTo("FTK2VoiceActing.Tests")` to access internal APIs
- `TestableAudioPlaybackHandle` subclasses `AudioPlaybackHandle` — override all 6 virtual methods to avoid Unity native API calls
- `RecreatingTestHandle` extends `TestableAudioPlaybackHandle` for scene-transition simulation
- `FakeVoicePlayback` implements `IVoicePlayback` with call counters and configurable clip/translation data
- `RuntimeHelpers.GetUninitializedObject(typeof(AudioClip))` creates non-null `AudioClip` references without invoking Unity constructors — never access their members
- Static patch state (`DialoguePatches.CurrentEmitter`, `VoiceManager` references) must be reset in `[SetUp]`/`[TearDown]`
- Temp directories are used for file-system tests and cleaned up in `[TearDown]`
