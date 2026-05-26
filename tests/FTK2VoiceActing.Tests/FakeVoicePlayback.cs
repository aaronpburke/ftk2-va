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
    }
}
