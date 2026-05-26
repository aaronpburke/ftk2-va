using System.Collections.Generic;

namespace FTK2VoiceActing.Tests
{
    /// <summary>
    /// Test double for IVoicePlayback that records play/stop calls.
    /// Used by both DialoguePatchTests and LoadingScreenPatchTests.
    /// </summary>
    internal class FakeVoicePlayback : IVoicePlayback
    {
        public int PlayCalls { get; private set; }
        public int StopCalls { get; private set; }
        public string LastNpcId { get; private set; }
        public string LastDialogueKey { get; private set; }

        // Maps (npcId, dialogueKey) for HasVoiceClip simulation
        private readonly HashSet<string> _clips = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        // Maps (npcId, translatedText) -> dialogueKey for FindKeyByTranslatedText simulation
        private readonly Dictionary<string, string> _translationMap = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        public void AddClip(string npcId, string dialogueKey)
        {
            _clips.Add(npcId + "|" + dialogueKey);
        }

        public void AddTranslation(string npcId, string translatedText, string dialogueKey)
        {
            _translationMap[npcId + "|" + translatedText] = dialogueKey;
        }

        public void PlayVoiceClip(string npcId, string dialogueKey)
        {
            PlayCalls++;
            LastNpcId = npcId;
            LastDialogueKey = dialogueKey;
        }

        public void StopCurrentClip()
        {
            StopCalls++;
        }

        public bool HasVoiceClip(string npcId, string dialogueKey)
        {
            return _clips.Contains(npcId + "|" + dialogueKey);
        }

        public string FindKeyByTranslatedText(string npcId, string translatedText)
        {
            return _translationMap.TryGetValue(npcId + "|" + translatedText, out var key) ? key : null;
        }
    }
}
