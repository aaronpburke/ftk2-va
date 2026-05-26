# FTK2VoiceActing — Build & Development Instructions

## Project Overview

This is a BepInEx 5 plugin for **For the King 2** that adds voice acting support to NPC dialogue and loading screen narration. It uses Harmony to patch the game's dialogue rendering methods and plays matching audio files from disk.

## Requirements

- [.NET SDK 6.0+](https://dotnet.microsoft.com/download) (for building)
- [Python 3.6+](https://www.python.org/downloads/) (for the dialogue extraction tool only)
- For the King 2 installed (any platform — Steam, GOG, Epic, etc.; needed for game assembly references)
- BepInEx 5.4.x installed in the game

## Quick Start

### 1. Populate the `lib/` folder

The project references game and BepInEx assemblies that are not distributed with the source. You must copy them manually from your local game installation.

See [`lib/README.md`](../lib/README.md) for the full list and a PowerShell one-liner to copy everything.

**Short version** (from the `lib/` directory):

```powershell
# Set this to your game installation directory
$game = "C:\Program Files (x86)\Steam\steamapps\common\For The King II"  # adjust for your platform
Copy-Item "$game\BepInEx\core\BepInEx.dll" .
Copy-Item "$game\BepInEx\core\0Harmony.dll" .
Copy-Item "$game\For The King II_Data\Managed\UnityEngine.dll" .
Copy-Item "$game\For The King II_Data\Managed\UnityEngine.CoreModule.dll" .
Copy-Item "$game\For The King II_Data\Managed\UnityEngine.AudioModule.dll" .
Copy-Item "$game\For The King II_Data\Managed\UnityEngine.UIElementsModule.dll" .
Copy-Item "$game\For The King II_Data\Managed\UnityEngine.UnityWebRequestAudioModule.dll" .
Copy-Item "$game\For The King II_Data\Managed\UnityEngine.UnityWebRequestModule.dll" .
Copy-Item "$game\For The King II_Data\Managed\FTK2.dll" .
```

### 2. Build

```bash
dotnet build src/FTK2VoiceActing/FTK2VoiceActing.csproj
```

For a release build:

```bash
dotnet build src/FTK2VoiceActing/FTK2VoiceActing.csproj -c Release
```

Output: `src/FTK2VoiceActing/bin/<config>/netstandard2.1/FTK2VoiceActing.dll`

### 3. Run Tests

```bash
dotnet test tests/FTK2VoiceActing.Tests/FTK2VoiceActing.Tests.csproj
```

### 4. Deploy for Testing

Copy the built DLL to your game's BepInEx plugins folder:

```powershell
$game = "C:\Program Files (x86)\Steam\steamapps\common\For The King II"  # adjust for your platform
$pluginDir = "$game\BepInEx\plugins\FTK2VoiceActing"
New-Item -ItemType Directory -Path $pluginDir -Force
Copy-Item "src\FTK2VoiceActing\bin\Debug\netstandard2.1\FTK2VoiceActing.dll" $pluginDir
```

## Project Structure

```
FTK2VoiceActing/
├── FTK2VoiceActing.sln              # Visual Studio solution
├── Directory.Build.props            # Shared build properties
├── README.md                        # End-user installation docs
├── VOICE_ACTOR_GUIDE.md             # Guide for voice actors
├── .gitignore
│
├── src/FTK2VoiceActing/             # Plugin source code
│   ├── FTK2VoiceActing.csproj       #   Project file (netstandard2.1)
│   ├── Plugin.cs                    #   BepInEx entry point
│   ├── VoiceConfig.cs               #   BepInEx configuration wrapper
│   ├── VoiceManager.cs              #   Audio file discovery & playback
│   ├── AudioPlaybackHandle.cs       #   Unity AudioSource lifecycle wrapper
│   ├── DialoguePatches.cs           #   Harmony patches for NPC dialogue
│   └── LoadingScreenPatches.cs      #   Harmony patches for narrator
│
├── tests/FTK2VoiceActing.Tests/     # NUnit test project
│   ├── FTK2VoiceActing.Tests.csproj #   Test project file (net8.0)
│   ├── VoiceManagerTests.cs         #   File discovery, paths, edge cases
│   ├── AudioPlaybackHandleTests.cs  #   Audio lifecycle, generation counter
│   ├── DialoguePatchTests.cs        #   Emitter tracking, key matching
│   ├── FakeVoicePlayback.cs         #   Test double for IVoicePlayback
│   ├── LoadingScreenPatchTests.cs   #   Narrator key logic
│   └── VoiceConfigTests.cs          #   Config defaults, clamping
│
├── tools/
│   └── ExtractDialogueLines.py      # Dialogue line extraction script
│
└── lib/                             # External assembly references
    └── README.md                    #   Instructions for populating
```

## Architecture

### Patch Flow

```
Game dialogue system                         Mod hooks
========================                     ===========================

ParseDialogueAction("EMITTER", "NPC_BARMAID")
  └─> DialogueViewHelper.RenderEmitter()  ──> [Prefix] Store NPC ID

ParseDialogueAction("SAY", <translated text>)
  └─> DialogueViewHelper.RenderSay()      ──> [Prefix] Stop any playing clip
                                                └─> Try direct key match
                                                └─> Reverse-lookup via Lang.__dt()
                                                └─> VoiceManager.PlayVoiceClip()
                                                     └─> Load .ogg/.wav async
                                                     └─> AudioSource.Play()

LoadingScreenViewHelper.Initialize("STORY_1_1")
  └─> [Postfix] Play NARRATOR/STORY_1_1_INTRO.ogg

LoadingScreenViewHelper.Hide()
  └─> [Prefix] Stop narrator audio

DialogueViewHelper.Deinitialize()
  └─> [Prefix] Stop dialogue audio and reset tracked speaker
```

### Key Classes

| Class | Purpose |
|---|---|
| `Plugin` | BepInEx entry point. Initializes config, VoiceManager, applies patches. |
| `VoiceConfig` | Wraps BepInEx `ConfigEntry` for volume, enable, debug logging. |
| `VoiceManager` | Scans `VoiceAssets/` directory, builds file index, loads and plays `AudioClip`s via `UnityWebRequestMultimedia`. Reverse-lookups translated text via `Lang.__dt()`. |
| `AudioPlaybackHandle` | Encapsulates Unity `AudioSource`/`GameObject` lifecycle with generation counter for async safety. Auto-recreates objects if Unity destroys them during scene transitions. |
| `DialoguePatches` | Harmony prefix patches on `RenderEmitter` (tracks speaker), `RenderSay` (stops previous clip, triggers playback via direct or reverse-translation match), and `Deinitialize` (stops dialogue audio). |
| `LoadingScreenPatches` | Harmony postfix on `Initialize` (starts narrator) and prefix on `Hide` (stops narrator). |

### External Dependencies

All external dependencies are consumed from the `lib/` folder as reference-only assemblies (`Private=false` in the .csproj). They are **not** included in the build output since they're already present at runtime in the game's directory.

| Assembly | Source |
|---|---|
| `BepInEx.dll` | BepInEx 5.4.x |
| `0Harmony.dll` | BepInEx (bundled HarmonyX) |
| `UnityEngine*.dll` | Game's `Managed/` folder |
| `FTK2.dll` | Game's `Managed/` folder |

## Game Analysis Notes

These notes document the game systems this mod interacts with, derived from the decompiled source at `D:\FTK2_Src`:

### Dialogue System

- Dialogues are JSON arrays of action dictionaries: `[{"EMITTER": "NPC_BARMAID", "SAY": "KEY"}, ...]`
- 652 dialogue files in `StreamingAssets/Assets/Configs/JSON~/Dialogues/`
- English text in `DialogueLangs/en.json` (1,337 keys)
- EMITTER always precedes SAY in the same action step
- `DialogueViewHelper.RenderSay(pValue, pDoTranslate)`:
  - The game pre-translates SAY values in the dialogue JSON before they reach `RenderSay`, so `pValue` is typically the translated English text even when `pDoTranslate=true`
  - The mod first tries a direct key match, then reverse-lookups through `Lang.__dt()` to find the original dialogue key from the translated text
  - EMITTER values are NOT translated — `pValue` is the raw NPC ID (e.g., `NPC_BARMAID`)

### Loading Screen

- `LoadingScreenViewHelper.Initialize(adventureID, root)` sets title and intro text
- Intro text key: `Lang.__t(adventureID + "_INTRO")` (e.g., `STORY_1_1_INTRO`)
- ~9 adventure intros in `Langs/en.json`

### Audio System

- Game uses custom `AudioPlayHelper` / `AudioSourceController` framework
- Our mod creates its own `AudioSource` to avoid coupling to game audio pipeline
- Files loaded via `UnityWebRequestMultimedia.GetAudioClip()` (supports .ogg, .wav)
