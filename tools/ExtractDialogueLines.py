#!/usr/bin/env python3
"""
ExtractDialogueLines.py — Extracts all dialogue lines from For the King 2 game data.

Generates a CSV reference sheet for voice actors showing:
- NPC ID (speaker)
- Dialogue Key (used as the audio file name)
- English Text (what the NPC says)
- Source Dialogue File (which dialogue script the line comes from)

Usage:
    python ExtractDialogueLines.py [--game-dir "C:\path\to\For The King II"] [--output dialogue_lines.csv]

The script reads from:
    <game-dir>/For The King II_Data/StreamingAssets/Assets/Configs/JSON~/Dialogues/*.json
    <game-dir>/For The King II_Data/StreamingAssets/Assets/Configs/JSON~/DialogueLangs/en.json
    <game-dir>/For The King II_Data/StreamingAssets/Assets/Configs/JSON~/Langs/en.json
"""

import argparse
import csv
import json
import os
import sys
from pathlib import Path


DEFAULT_GAME_DIR = r"C:\Program Files (x86)\Steam\steamapps\common\For The King II"


def find_configs_path(game_dir: str) -> Path:
    """Locate the JSON configs directory within the game installation."""
    configs = Path(game_dir) / "For The King II_Data" / "StreamingAssets" / "Assets" / "Configs" / "JSON~"
    if not configs.exists():
        raise FileNotFoundError(f"Configs directory not found: {configs}")
    return configs


def load_dialogue_translations(configs_path: Path) -> dict:
    """Load the English dialogue translations (DialogueLangs/en.json)."""
    lang_file = configs_path / "DialogueLangs" / "en.json"
    if not lang_file.exists():
        print(f"Warning: Dialogue translations not found: {lang_file}", file=sys.stderr)
        return {}

    with open(lang_file, "r", encoding="utf-8") as f:
        return json.load(f)


def load_general_translations(configs_path: Path) -> dict:
    """Load the English general translations (Langs/en.json) for adventure intros."""
    lang_file = configs_path / "Langs" / "en.json"
    if not lang_file.exists():
        print(f"Warning: General translations not found: {lang_file}", file=sys.stderr)
        return {}

    with open(lang_file, "r", encoding="utf-8") as f:
        return json.load(f)


def extract_dialogue_lines(configs_path: Path, dialogue_translations: dict):
    """
    Parse all dialogue JSON files and extract lines with their NPC speakers.

    Yields tuples of (npc_id, dialogue_key, english_text, source_file).
    """
    dialogues_dir = configs_path / "Dialogues"
    if not dialogues_dir.exists():
        print(f"Warning: Dialogues directory not found: {dialogues_dir}", file=sys.stderr)
        return

    for json_file in sorted(dialogues_dir.glob("*.json")):
        if json_file.name == ".json":
            continue

        try:
            with open(json_file, "r", encoding="utf-8") as f:
                dialogue_script = json.load(f)
        except (json.JSONDecodeError, UnicodeDecodeError) as e:
            print(f"Warning: Failed to parse {json_file.name}: {e}", file=sys.stderr)
            continue

        if not isinstance(dialogue_script, list):
            continue

        current_emitter = None
        source_name = json_file.stem

        for action_dict in dialogue_script:
            if not isinstance(action_dict, dict):
                continue

            # Track the current speaker
            if "EMITTER" in action_dict:
                emitter_value = action_dict["EMITTER"]
                if isinstance(emitter_value, str):
                    current_emitter = emitter_value

            # Extract SAY lines
            if "SAY" in action_dict:
                say_key = action_dict["SAY"]
                if isinstance(say_key, str) and say_key:
                    english_text = dialogue_translations.get(say_key, "[TEXT NOT FOUND]")
                    npc_id = current_emitter if current_emitter else "[UNKNOWN]"
                    yield (npc_id, say_key, english_text, source_name)


def extract_narrator_lines(configs_path: Path, general_translations: dict):
    """
    Extract adventure intro lines that are spoken by the narrator on loading screens.

    The game uses Lang.__t(adventureID + "_INTRO") for loading screen body text.
    Yields tuples of (npc_id, dialogue_key, english_text, source_file).
    """
    intro_keys = [
        key for key in general_translations.keys()
        if key.endswith("_INTRO")
    ]

    for key in sorted(intro_keys):
        english_text = general_translations[key]
        yield ("NARRATOR", key, english_text, "Langs/en.json")


def create_folder_template(output_dir: str, lines):
    """Create an empty folder structure template for voice actors."""
    template_dir = Path(output_dir) / "VoiceAssets_Template"

    npc_ids = set()
    for npc_id, _, _, _ in lines:
        if npc_id and npc_id != "[UNKNOWN]":
            npc_ids.add(npc_id)

    for npc_id in sorted(npc_ids):
        npc_dir = template_dir / npc_id
        npc_dir.mkdir(parents=True, exist_ok=True)

    print(f"Created folder template with {len(npc_ids)} NPC directories in: {template_dir}")


def main():
    parser = argparse.ArgumentParser(
        description="Extract dialogue lines from For the King 2 for voice acting reference."
    )
    parser.add_argument(
        "--game-dir",
        default=DEFAULT_GAME_DIR,
        help=f"Path to the game installation directory (default: {DEFAULT_GAME_DIR})"
    )
    parser.add_argument(
        "--output",
        default="dialogue_lines.csv",
        help="Output CSV file path (default: dialogue_lines.csv)"
    )
    parser.add_argument(
        "--create-folders",
        action="store_true",
        help="Also create an empty folder template structure for voice actors"
    )

    args = parser.parse_args()

    try:
        configs_path = find_configs_path(args.game_dir)
    except FileNotFoundError as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)

    print(f"Reading configs from: {configs_path}")

    dialogue_translations = load_dialogue_translations(configs_path)
    general_translations = load_general_translations(configs_path)

    print(f"Loaded {len(dialogue_translations)} dialogue translations")
    print(f"Loaded {len(general_translations)} general translations")

    # Collect all lines
    all_lines = []

    # NPC dialogue lines
    for line in extract_dialogue_lines(configs_path, dialogue_translations):
        all_lines.append(line)

    # Narrator lines
    for line in extract_narrator_lines(configs_path, general_translations):
        all_lines.append(line)

    # Write CSV
    with open(args.output, "w", newline="", encoding="utf-8") as csvfile:
        writer = csv.writer(csvfile)
        writer.writerow(["NPC_ID", "DialogueKey", "EnglishText", "SourceFile"])

        for npc_id, key, text, source in all_lines:
            # Clean up text for CSV (remove color tags, special chars)
            clean_text = text.replace("\n", " ").replace("\u200b", "")
            writer.writerow([npc_id, key, clean_text, source])

    # Summary statistics
    npc_counts = {}
    for npc_id, _, _, _ in all_lines:
        npc_counts[npc_id] = npc_counts.get(npc_id, 0) + 1

    print(f"\nExtracted {len(all_lines)} total dialogue lines to: {args.output}")
    print(f"Unique speakers: {len(npc_counts)}")
    print("\nLines per speaker:")
    for npc_id in sorted(npc_counts.keys()):
        print(f"  {npc_id}: {npc_counts[npc_id]} lines")

    if args.create_folders:
        output_dir = os.path.dirname(os.path.abspath(args.output))
        create_folder_template(output_dir, all_lines)


if __name__ == "__main__":
    main()
