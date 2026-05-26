# AI Agent Onboarding — FTK2 Voice Acting Mod

This document captures hard-won knowledge for any AI agent working on this codebase. Read this before making changes.

## Quick Reference

| Item | Value |
|---|---|
| Framework | BepInEx 5.4.x (Mono backend) |
| Target | .NET Standard 2.1 |
| Unity | 2022.3.41 |
| Game | For the King 2 (`For The King II.exe`) |
| Solution | `FTK2VoiceActing.sln` |
| Plugin GUID | `dev.ftk2.voiceacting` |
| Decompiled source | External, at user's discretion (originally `D:\FTK2_Src`) |
| Build | `dotnet build` (no restore needed after first time) |
| Test | `dotnet test` — 111 NUnit tests, all must pass |
| Deploy | Copy `src/.../bin/Debug/netstandard2.1/FTK2VoiceActing.dll` to game's `BepInEx/plugins/FTK2VoiceActing/` |

## Architecture

```
Plugin.cs (BepInEx entry)
  ├── VoiceConfig.cs          — ConfigEntry wrappers (Enabled, Volume, DebugLogging)
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

### 1. SAY values are pre-translated

The game's dialogue JSON has `"SAY": "STORY_1_1_VISIT_PRAN_PRE_DIALOG_1"` (a translation key), but by the time `DialogueViewHelper.RenderSay(pValue, pDoTranslate)` is called, `pValue` is **already the translated English text** (e.g., `"My, we haven't seen many adventurers..."`), even when `pDoTranslate=true`.

**EMITTER values are NOT translated** — `pValue` is the raw NPC ID like `NPC_BARMAID`.

The mod handles this with a two-step lookup in `RenderSayPrefix`:
1. Try direct key match (`HasVoiceClip(emitter, pValue)`) — works if pValue is somehow a raw key
2. Reverse-lookup via `VoiceManager.FindKeyByTranslatedText()` which calls `Lang.__dt(key)` on each indexed key for the NPC and compares the result to the received text

### 2. Unity destroys DontDestroyOnLoad objects during scene transitions

Unity calls `OnDestroy` on `MonoBehaviour` instances attached to `DontDestroyOnLoad` GameObjects during the initial scene load. It also destroys standalone `GameObject`s created during `Awake()`.

**Consequences and fixes:**
- `Plugin.OnDestroy()` has a `_quitting` guard — only cleans up when `OnApplicationQuit()` has fired
- `AudioPlaybackHandle.Play()` calls `EnsureCreated()` which detects destroyed Unity objects (via Unity's overloaded `== null`) and recreates them automatically
- `StopUnityPlayback()` and `PlayUnityClip()` have `if (_source == null) return;` guards
- The `_created` flag tracks logical state; `IsUnityObjectDestroyed()` checks actual native object liveness

### 3. RenderSay must always stop the previous clip

When the player advances dialogue, `RenderSayPrefix` fires for the next line. If no voice file exists for that line, the previous clip must still be stopped. The stop call is at the **top** of `RenderSayPrefix`, before any key matching.

### 4. LoadAndPlayClip must not check IsReady

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
- All 5 Unity-facing methods in `AudioPlaybackHandle` are `protected virtual` for test overriding

## External Assembly References

The `lib/` folder must be populated manually with DLLs from the game installation. See `lib/README.md` for the full list. These are reference-only (`Private=false`) — not copied to output.

Required assemblies:
- `BepInEx.dll`, `0Harmony.dll` — from `BepInEx/core/`
- `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.AudioModule.dll`, `UnityEngine.UIElementsModule.dll`, `UnityEngine.UnityWebRequestAudioModule.dll`, `UnityEngine.UnityWebRequestModule.dll` — from game's `Managed/`
- `FTK2.dll` — from game's `Managed/` (contains `DialogueViewHelper`, `LoadingScreenViewHelper`, `Lang`)

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

## Common Pitfalls

1. **Don't add an `IsReady` guard before async audio loading** — `Play()` handles recreation
2. **Don't use `clip == null`** in test code — use `ReferenceEquals(clip, null)` to avoid Unity native calls
3. **Don't clean up in `OnDestroy` without a quit guard** — Unity fires it during scene transitions
4. **Don't assume `_created` means objects are alive** — always check via `IsUnityObjectDestroyed()`
5. **Don't forget to stop audio on SAY** — every `RenderSayPrefix` must stop the previous clip unconditionally
6. **Don't commit files to `VoiceAssets_Template/`** — `.gitignore` excludes `.ogg`, `.wav`, and generated CSVs
7. **Don't add direct project references to the game source** — all game types come from `lib/*.dll`

## Useful Decompiled Source Files

If you have access to the decompiled game source:

| File | What it tells you |
|---|---|
| `GameplayDirectorBase.cs` (line ~1965) | `ParseDialogueAction` dispatcher — how SAY/EMITTER reach `RenderSay`/`RenderEmitter` |
| `DialogueViewHelper.cs` | `RenderSay`, `RenderEmitter`, `Deinitialize` signatures |
| `LoadingScreenViewHelper.cs` | `Initialize(string, VisualElement)`, `Hide(Action)` signatures |
| `Lang.cs` | `__t()` (translate), `__dt()` (dialogue translate) — used for reverse lookup |
| `StreamingAssets/Assets/Configs/JSON~/Dialogues/` | 652 dialogue JSON files |
| `DialogueLangs/en.json` | 1,337 dialogue translation keys |
| `Langs/en.json` | 9,630 general translation keys |

## Build / Test / Deploy Cycle

```bash
dotnet build --no-restore                    # Build (< 2 seconds)
dotnet test --no-build                       # Test (< 1 second, 111 tests)
# Deploy:
cp src/FTK2VoiceActing/bin/Debug/netstandard2.1/FTK2VoiceActing.dll \
   "<game>/BepInEx/plugins/FTK2VoiceActing/"
# Check logs after running game:
cat "<game>/BepInEx/LogOutput.log" | grep "FTK2 Voice Acting"
```
