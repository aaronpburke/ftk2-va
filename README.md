# FTK2 Voice Acting Mod

A BepInEx plugin that adds voice acting support to **For the King 2** NPC dialogue lines and loading screen narration.

Voice actors record audio files and place them in a simple folder structure. The mod automatically plays the matching audio when dialogue appears in-game.

## Prerequisites

- **For the King 2** (any platform — Steam, GOG, Epic, etc.)
- **BepInEx 5.4.x** installed for the game ([BepInEx installation guide](https://docs.bepinex.dev/articles/user_guide/installation/index.html))

## Installation

1. **Download** the latest `FTK2VoiceActing.dll` from the releases page.

2. **Create the plugin folder** inside your BepInEx installation:

   ```
   <game folder>/BepInEx/plugins/FTK2VoiceActing/
   ```

3. **Copy the DLL** into the plugin folder:

   ```
   <game folder>/BepInEx/plugins/FTK2VoiceActing/FTK2VoiceActing.dll
   ```

4. **Create the VoiceAssets folder** for audio files:

   ```
   <game folder>/BepInEx/plugins/FTK2VoiceActing/VoiceAssets/
   ```

5. **Add voice files** organized by NPC speaker:

   ```
   VoiceAssets/
   ├── NPC_BARMAID/
   │   ├── STORY_1_1_VISIT_PRAN_PRE_DIALOG_1.ogg
   │   ├── STORY_1_1_VISIT_PRAN_PRE_DIALOG_2.ogg
   │   └── ...
   ├── NPC_HILDEBRANT/
   │   ├── STORY_1_1_CLEAR_GAMBLING_DEN_POST_DIALOG_1.wav
   │   └── ...
   ├── NARRATOR/
   │   ├── STORY_1_1_INTRO.ogg
   │   ├── STORY_1_2_INTRO.ogg
   │   └── ...
   └── ...
   ```

6. **Launch the game**. Voice clips will play automatically when matching dialogue appears.

## Folder Structure Summary

```
<game folder>/
└── BepInEx/
    └── plugins/
        └── FTK2VoiceActing/
            ├── FTK2VoiceActing.dll          ← The plugin
            └── VoiceAssets/                 ← Your voice audio files
                ├── NPC_BARMAID/             ← One folder per NPC
                │   └── <DIALOGUE_KEY>.ogg   ← Audio file named by dialogue key
                ├── NARRATOR/                ← Loading screen narration
                │   └── STORY_1_1_INTRO.ogg
                └── ...
```

## Audio Format Requirements

- **Supported formats**: `.ogg` (OGG Vorbis) and `.wav`
- **Recommended**: `.ogg` for smaller file sizes
- **Sample rate**: 44100 Hz or 48000 Hz
- **Channels**: Mono or Stereo (mono recommended for dialogue)
- If both `.ogg` and `.wav` exist for the same key, `.ogg` is preferred

## Configuration

After first launch, a config file is created at:

```
<game folder>/BepInEx/config/dev.ftk2.voiceacting.cfg
```

### Available Settings

| Setting | Default | Description |
|---|---|---|
| `Enabled` | `true` | Enable or disable voice acting playback |
| `Volume` | `1.0` | Voice acting volume (0.0 = silent, 1.0 = full) |
| `DebugLogging` | `false` | Enable verbose debug logging to BepInEx console |

### Example Config

```ini
[General]
Enabled = true
Volume = 0.8
DebugLogging = false
```

## How It Works

The mod uses [Harmony](https://github.com/pardeike/Harmony) to patch key game methods:

1. **`DialogueViewHelper.RenderEmitter`** — Tracks which NPC is currently speaking
2. **`DialogueViewHelper.RenderSay`** — When dialogue text is displayed, plays the matching audio file
3. **`DialogueViewHelper.Deinitialize`** — Stops voice when dialogue closes
4. **`LoadingScreenViewHelper.Initialize`** — Plays narrator audio for adventure loading screen intros
5. **`LoadingScreenViewHelper.Hide`** — Stops narrator audio when loading screen closes

When a dialogue line with key `STORY_1_1_VISIT_PRAN_PRE_DIALOG_1` is spoken by `NPC_BARMAID`, the mod looks for:

```
VoiceAssets/NPC_BARMAID/STORY_1_1_VISIT_PRAN_PRE_DIALOG_1.ogg
```

or

```
VoiceAssets/NPC_BARMAID/STORY_1_1_VISIT_PRAN_PRE_DIALOG_1.wav
```

## Troubleshooting

### No voice clips are playing

1. Check that BepInEx is loading correctly. Look for `BepInEx/LogOutput.log` in the game folder.
2. Search the log for `FTK2 Voice Acting` — you should see:
   ```
   [Info   :FTK2 Voice Acting] FTK2 Voice Acting v1.0.0 loading...
   [Info   :FTK2 Voice Acting] FTK2 Voice Acting loaded. X voice files indexed.
   ```
3. If it says `0 voice files indexed`, check that your `VoiceAssets` folder is in the right location.
4. Enable `DebugLogging = true` in the config to see detailed file lookup messages.

### VoiceAssets directory not found warning

The `VoiceAssets` folder must be in the same directory as `FTK2VoiceActing.dll`:

```
BepInEx/plugins/FTK2VoiceActing/FTK2VoiceActing.dll
BepInEx/plugins/FTK2VoiceActing/VoiceAssets/   ← Must be here
```

**Not** at:

```
BepInEx/plugins/FTK2VoiceActing.dll            ← Wrong location
BepInEx/plugins/VoiceAssets/                   ← Wrong location
```

### Audio plays but sounds wrong

- Check that your audio files are valid `.ogg` or `.wav` files
- Ensure the sample rate is 44100 Hz or 48000 Hz
- Try adjusting the `Volume` setting in the config

## Building from Source

See [lib/README.md](lib/README.md) for instructions on setting up external assembly references, then:

```bash
dotnet build src/FTK2VoiceActing/FTK2VoiceActing.csproj -c Release
```

The output DLL will be at: `src/FTK2VoiceActing/bin/Release/netstandard2.1/FTK2VoiceActing.dll`

## Running Tests

```bash
dotnet test tests/FTK2VoiceActing.Tests/FTK2VoiceActing.Tests.csproj
```

## License

This mod is licensed under the Apache 2.0 license. See LICENSE for details.
