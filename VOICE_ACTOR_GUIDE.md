# Voice Actor Guide — For the King 2 Voice Acting Mod

This guide explains how to record and package voice acting files for the FTK2 Voice Acting mod. No modding experience is required!

## Overview

The mod plays audio files when NPC dialogue text appears on screen. Each dialogue line in the game has a unique **dialogue key** (like `STORY_1_1_VISIT_PRAN_PRE_DIALOG_1`). You record audio for each line and save it as a file named after that key.

There are two types of voice roles:

1. **NPC Dialogue** — Character voices for in-game NPC conversations
2. **Narrator** — Loading screen narration read between missions

## Step 1: Get the Dialogue Reference Sheet

A Python script is included to extract all dialogue lines into a CSV spreadsheet.

### Requirements

- Python 3.6 or newer ([download here](https://www.python.org/downloads/))
- For the King 2 installed (any platform — Steam, GOG, Epic, etc.)

### Running the Extraction Tool

Open a command prompt or terminal and run:

```bash
cd <path-to-mod-source>/tools
python ExtractDialogueLines.py --create-folders
```

If your game is installed in a non-default location:

```bash
python ExtractDialogueLines.py --game-dir "D:\Games\For The King II" --create-folders
```

This creates two things:

1. **`dialogue_lines.csv`** — A spreadsheet with all dialogue lines
2. **`VoiceAssets_Template/`** — An empty folder structure ready for your recordings

### Understanding the CSV

Open `dialogue_lines.csv` in Excel, Google Sheets, or any spreadsheet app. It has four columns:

| Column | Description | Example |
|---|---|---|
| `NPC_ID` | The speaker's internal ID | `NPC_BARMAID` |
| `DialogueKey` | The unique key for this line (your file name) | `STORY_1_1_VISIT_PRAN_PRE_DIALOG_1` |
| `EnglishText` | What the NPC says | `"Well, this is the Pran tavern..."` |
| `SourceFile` | Which dialogue script this comes from | `STORY_1_1_VISIT_PRAN_PRE_DIALOG` |

**Tip:** Sort the CSV by `NPC_ID` to group all lines by character. This makes it easier to record one character at a time.

## Step 2: Record Your Audio

### Audio Requirements

| Setting | Value |
|---|---|
| **Format** | `.ogg` (recommended) or `.wav` |
| **Sample rate** | 44100 Hz or 48000 Hz |
| **Channels** | Mono (recommended) or Stereo |
| **Bit depth** | 16-bit (for WAV) |

### Recording Tips

- Use a quiet room with minimal background noise
- Keep a consistent distance from your microphone
- Record at a comfortable speaking volume — the mod has a volume slider
- For NPC lines, try to match the character's personality from the text
- For narrator lines, use a clear, measured reading pace — these are longer expositional passages
- Leave a small amount of silence (0.2–0.5 seconds) at the start and end of each clip
- You don't need to record every line! The mod gracefully skips missing files

### Recommended Free Software

- **[Audacity](https://www.audacityteam.org/)** — Free audio recording and editing
  - Record, trim silence, export as OGG
  - File → Export → Export as OGG
- **[OBS Studio](https://obsproject.com/)** — Can also record audio
- **[FFmpeg](https://ffmpeg.org/)** — Command-line tool for batch audio conversion (see below)

## Step 3: Name and Organize Your Files

### File Naming

Each audio file must be named exactly after the `DialogueKey` from the CSV:

```
STORY_1_1_VISIT_PRAN_PRE_DIALOG_1.ogg
```

**Important:** The file name must match the key **exactly** (it is case-insensitive, so `story_1_1_visit_pran_pre_dialog_1.ogg` also works).

### Folder Structure

Place each file in a folder named after the NPC who speaks the line:

```
VoiceAssets/
├── NPC_BARMAID/
│   ├── STORY_1_1_VISIT_PRAN_PRE_DIALOG_1.ogg
│   ├── STORY_1_1_VISIT_PRAN_PRE_DIALOG_2.ogg
│   ├── STORY_1_1_VISIT_PRAN_PRE_DIALOG_3.ogg
│   └── ...
├── NPC_HILDEBRANT/
│   ├── STORY_1_1_CLEAR_GAMBLING_DEN_POST_DIALOG_1.ogg
│   └── ...
└── NARRATOR/
    ├── STORY_1_1_INTRO.ogg
    ├── STORY_1_2_INTRO.ogg
    └── ...
```

**If you used `--create-folders`**, the `VoiceAssets_Template/` directory already has all the NPC folders created for you. Just copy your audio files into the right folders.

## Step 4: Install and Test

1. Copy your `VoiceAssets/` folder to the mod's plugin directory:

   ```
   <game folder>/BepInEx/plugins/FTK2VoiceActing/VoiceAssets/
   ```

2. Launch For the King 2

3. Start a campaign and trigger a dialogue scene. Your voice clip should play automatically when the text appears!

4. Check the BepInEx log for any issues:
   - Open `<game folder>/BepInEx/LogOutput.log`
   - Search for `FTK2 Voice Acting`
   - Look for messages like `Loading voice clip: ...` and `Voice clip playing: ...`

### Testing Tips

- Start with just a few files for one NPC to confirm everything works
- The mod logs how many files it indexed at startup — check this first
- If a file isn't playing, enable `DebugLogging = true` in the config at:
  ```
  <game folder>/BepInEx/config/dev.ftk2.voiceacting.cfg
  ```

## Example Walkthrough: Recording NPC_BARMAID

Let's walk through recording lines for the Barmaid (Hildegard), one of the main NPCs.

### 1. Filter the CSV

Open `dialogue_lines.csv` and filter by `NPC_ID = NPC_BARMAID`. You'll see ~54 lines like:

| DialogueKey | EnglishText |
|---|---|
| `STORY_1_1_VISIT_PRAN_PRE_DIALOG_1` | "Well, this is the Pran tavern..." |
| `STORY_1_1_VISIT_PRAN_PRE_DIALOG_2` | "I'm Hildegard, the barmaid..." |
| ... | ... |

### 2. Record in Audacity

1. Open Audacity
2. Read the first line in character
3. File → Export → Export as OGG
4. Save as `STORY_1_1_VISIT_PRAN_PRE_DIALOG_1.ogg`
5. Repeat for each line

### 3. Organize Files

```
VoiceAssets/
└── NPC_BARMAID/
    ├── STORY_1_1_VISIT_PRAN_PRE_DIALOG_1.ogg
    ├── STORY_1_1_VISIT_PRAN_PRE_DIALOG_2.ogg
    └── ... (all 54 files)
```

### 4. Install and Play

Copy `VoiceAssets/` to `BepInEx/plugins/FTK2VoiceActing/VoiceAssets/`, launch the game, and start Story 1-1. When you visit the Pran tavern, your voice recordings will play!

## Character Reference

Here are the main characters with the most dialogue lines:

| NPC ID | Character | Lines |
|---|---|---|
| `NPC_GRIZELLE` | Grizelle | 110 |
| `NPC_HILDEBRANT` | Hildebrant | 92 |
| `NPC_HILDEBRANT_AGENT` | Hildebrant (Agent) | 77 |
| `NPC_JEREMY_KING` | Jeremy (King) | 65 |
| `NPC_ROSOMON_VOID` | Rosomon (Void) | 62 |
| `NPC_BARMAID` | Hildegard (Barmaid) | 54 |
| `NPC_PINECONE` | Pinecone | 47 |
| `NPC_RINGMASTER` | Ringmaster | 46 |
| `NPC_HEARTCRUSHER` | Heartcrusher | 39 |
| `NPC_ROSOMON` | Queen Rosomon | 37 |
| `NPC_PORTER` | Porter | 35 |
| `NPC_HAYPENNY` | Haypenny | 35 |
| `NPC_MOONEY` | Mooney | 32 |
| `NPC_VOID_GUIDE` | Void Guide | 28 |
| `NPC_OMUS_HEAD` | Omus Head | 26 |
| `NPC_QUORIX` | Quorix | 26 |
| `NPC_ROSOMON_MAD` | Rosomon (Mad) | 23 |
| `NARRATOR` | Narrator (loading screens) | 9 |

## FAQ

**Q: Do I have to record every line?**
No! The mod gracefully handles missing files. Record as many or as few lines as you want. Start with one mission's worth of lines and expand from there.

**Q: Can I record just one character?**
Absolutely! Each NPC folder is independent. You can record just the Barmaid, or just the Narrator — it's up to you.

**Q: What if my file name doesn't match?**
The mod won't find the file and will silently skip it. Double-check the `DialogueKey` column in the CSV — the file name must match exactly (though case doesn't matter).

**Q: Can I use MP3 files?**
No, only `.ogg` and `.wav` are supported. Most audio editors can export to OGG. If you record in WAV, you can batch-convert to OGG using the helper scripts (requires [ffmpeg](https://ffmpeg.org/)):

```bash
# macOS/Linux/Git Bash
./tools/convert_audio.sh recordings/ VoiceAssets/NPC_BARMAID/
```

```powershell
# Windows PowerShell
.\tools\ConvertAudio.ps1 -InputDir recordings -OutputDir VoiceAssets\NPC_BARMAID
```

**Q: How do I adjust the volume?**
Edit the config file at `BepInEx/config/dev.ftk2.voiceacting.cfg` and change the `Volume` setting (0.0 to 1.0).

**Q: The text has weird formatting like `<color={{RED}}>`. What should I read?**
Ignore the formatting tags. Just read the actual dialogue text naturally. For example, for `<color={{RED}}>Queensguard</color>`, just say "Queensguard".

**Q: The text has `{{QUEST_OBJECTIVE_0_ENTITY_NAME}}`. What do I say?**
These are placeholder variables that get replaced with different values at runtime. You can either skip these lines or record a generic version. The text before and after the placeholder is usually enough context.
