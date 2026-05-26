# External Assembly References

This folder must contain the following DLLs before building the project.
These are **not** distributed with the mod source code — you must copy them from your local game installation.

## From BepInEx (`<game>/BepInEx/core/`)

| File | Purpose |
|---|---|
| `BepInEx.dll` | BepInEx plugin framework |
| `0Harmony.dll` | HarmonyX method patching |

## From the game's Managed folder (`<game>/For The King II_Data/Managed/`)

| File | Purpose |
|---|---|
| `UnityEngine.dll` | Unity engine base |
| `UnityEngine.CoreModule.dll` | Core Unity types (MonoBehaviour, GameObject, etc.) |
| `UnityEngine.AudioModule.dll` | AudioSource, AudioClip |
| `UnityEngine.UIElementsModule.dll` | UI Toolkit types (VisualElement, Label) |
| `UnityEngine.UnityWebRequestAudioModule.dll` | UnityWebRequestMultimedia for loading audio |
| `UnityEngine.UnityWebRequestModule.dll` | UnityWebRequest base types |
| `FTK2.dll` | Game assembly (DialogueViewHelper, LoadingScreenViewHelper, Lang, etc.) |

## Quick Setup

Set `$game` to your game installation directory and run these commands from this directory:

```powershell
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
