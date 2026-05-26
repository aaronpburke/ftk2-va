using NUnit.Framework;

namespace FTK2VoiceActing.Tests
{
    [TestFixture]
    public class DialoguePatchTests
    {
        [SetUp]
        public void SetUp()
        {
            DialoguePatches.Reset();
            DialoguePatches.VoiceManager = null;
        }

        // --- RenderEmitterPrefix tests ---

        [Test]
        public void RenderEmitterPrefix_SetsCurrentEmitter_WhenDoTranslateTrue()
        {
            DialoguePatches.RenderEmitterPrefix("NPC_BARMAID", pDoTranslate: true);
            Assert.AreEqual("NPC_BARMAID", DialoguePatches.CurrentEmitter);
        }

        [Test]
        public void RenderEmitterPrefix_DoesNotSetEmitter_WhenDoTranslateFalse()
        {
            DialoguePatches.CurrentEmitter = "EXISTING";
            DialoguePatches.RenderEmitterPrefix("Some Display Name", pDoTranslate: false);
            Assert.AreEqual("EXISTING", DialoguePatches.CurrentEmitter);
        }

        [Test]
        public void RenderEmitterPrefix_DoesNotSetEmitter_WhenNull()
        {
            DialoguePatches.CurrentEmitter = "EXISTING";
            DialoguePatches.RenderEmitterPrefix(null, pDoTranslate: true);
            Assert.AreEqual("EXISTING", DialoguePatches.CurrentEmitter);
        }

        [Test]
        public void RenderEmitterPrefix_DoesNotSetEmitter_WhenEmpty()
        {
            DialoguePatches.CurrentEmitter = "EXISTING";
            DialoguePatches.RenderEmitterPrefix("", pDoTranslate: true);
            Assert.AreEqual("EXISTING", DialoguePatches.CurrentEmitter);
        }

        [Test]
        public void RenderEmitterPrefix_OverwritesPreviousEmitter()
        {
            DialoguePatches.RenderEmitterPrefix("NPC_BARMAID", pDoTranslate: true);
            Assert.AreEqual("NPC_BARMAID", DialoguePatches.CurrentEmitter);

            DialoguePatches.RenderEmitterPrefix("NPC_HILDEBRANT", pDoTranslate: true);
            Assert.AreEqual("NPC_HILDEBRANT", DialoguePatches.CurrentEmitter);
        }

        // --- RenderSayPrefix tests ---

        [Test]
        public void RenderSayPrefix_DoesNotThrow_WhenNoVoiceManager()
        {
            DialoguePatches.VoiceManager = null;
            DialoguePatches.CurrentEmitter = "NPC_BARMAID";
            Assert.DoesNotThrow(() => DialoguePatches.RenderSayPrefix("DIALOG_KEY", pDoTranslate: true));
        }

        [Test]
        public void RenderSayPrefix_PlaysVoice_WhenEmitterAndTranslationKeyAreAvailable()
        {
            var playback = new FakeVoicePlayback();
            DialoguePatches.VoiceManager = playback;
            DialoguePatches.CurrentEmitter = "NPC_BARMAID";

            DialoguePatches.RenderSayPrefix("DIALOG_KEY", pDoTranslate: true);

            Assert.AreEqual(1, playback.PlayCalls);
            Assert.AreEqual("NPC_BARMAID", playback.LastNpcId);
            Assert.AreEqual("DIALOG_KEY", playback.LastDialogueKey);
        }

        [Test]
        public void RenderSayPrefix_DoesNotThrow_WhenNullValue()
        {
            DialoguePatches.CurrentEmitter = "NPC_BARMAID";
            Assert.DoesNotThrow(() => DialoguePatches.RenderSayPrefix(null, pDoTranslate: true));
        }

        [Test]
        public void RenderSayPrefix_DoesNotThrow_WhenEmptyValue()
        {
            DialoguePatches.CurrentEmitter = "NPC_BARMAID";
            Assert.DoesNotThrow(() => DialoguePatches.RenderSayPrefix("", pDoTranslate: true));
        }

        [Test]
        public void RenderSayPrefix_DoesNotThrow_WhenNoEmitter()
        {
            DialoguePatches.CurrentEmitter = null;
            Assert.DoesNotThrow(() => DialoguePatches.RenderSayPrefix("DIALOG_KEY", pDoTranslate: true));
        }

        [Test]
        public void RenderSayPrefix_SkipsWhenDoTranslateFalse()
        {
            var playback = new FakeVoicePlayback();
            DialoguePatches.VoiceManager = playback;
            // When pDoTranslate is false, the value is already resolved text — we skip
            DialoguePatches.CurrentEmitter = "NPC_BARMAID";
            // Should not throw and should not attempt playback
            Assert.DoesNotThrow(() => DialoguePatches.RenderSayPrefix("Some resolved text", pDoTranslate: false));
            Assert.AreEqual(0, playback.PlayCalls);
        }

        // --- Emitter tracking state machine tests ---

        [Test]
        public void EmitterPersistsAcrossMultipleSayLines()
        {
            // Simulate: EMITTER sets NPC, then multiple SAY lines use that emitter
            DialoguePatches.RenderEmitterPrefix("NPC_BARMAID", pDoTranslate: true);
            Assert.AreEqual("NPC_BARMAID", DialoguePatches.CurrentEmitter);

            // First SAY — emitter should still be NPC_BARMAID
            DialoguePatches.RenderSayPrefix("DIALOG_1", pDoTranslate: true);
            Assert.AreEqual("NPC_BARMAID", DialoguePatches.CurrentEmitter);

            // Second SAY without a new EMITTER — emitter should persist
            DialoguePatches.RenderSayPrefix("DIALOG_2", pDoTranslate: true);
            Assert.AreEqual("NPC_BARMAID", DialoguePatches.CurrentEmitter);
        }

        [Test]
        public void EmitterChanges_MidDialogue()
        {
            // First speaker
            DialoguePatches.RenderEmitterPrefix("NPC_BARMAID", pDoTranslate: true);
            Assert.AreEqual("NPC_BARMAID", DialoguePatches.CurrentEmitter);

            DialoguePatches.RenderSayPrefix("DIALOG_1", pDoTranslate: true);

            // Speaker changes mid-dialogue
            DialoguePatches.RenderEmitterPrefix("NPC_HILDEBRANT", pDoTranslate: true);
            Assert.AreEqual("NPC_HILDEBRANT", DialoguePatches.CurrentEmitter);

            DialoguePatches.RenderSayPrefix("DIALOG_2", pDoTranslate: true);
            Assert.AreEqual("NPC_HILDEBRANT", DialoguePatches.CurrentEmitter);
        }

        // --- Reset tests ---

        [Test]
        public void Reset_ClearsEmitter()
        {
            DialoguePatches.CurrentEmitter = "NPC_BARMAID";
            DialoguePatches.Reset();
            Assert.IsNull(DialoguePatches.CurrentEmitter);
        }

        // --- DeinitializePrefix tests ---

        [Test]
        public void DeinitializePrefix_ResetsEmitter()
        {
            DialoguePatches.CurrentEmitter = "NPC_BARMAID";
            DialoguePatches.DeinitializePrefix();
            Assert.IsNull(DialoguePatches.CurrentEmitter);
        }

        [Test]
        public void DeinitializePrefix_DoesNotThrow_WhenNoVoiceManager()
        {
            DialoguePatches.VoiceManager = null;
            DialoguePatches.CurrentEmitter = "NPC_BARMAID";
            Assert.DoesNotThrow(() => DialoguePatches.DeinitializePrefix());
        }

        [Test]
        public void DeinitializePrefix_StopsVoice_WhenVoiceManagerIsSet()
        {
            var playback = new FakeVoicePlayback();
            DialoguePatches.VoiceManager = playback;
            DialoguePatches.CurrentEmitter = "NPC_BARMAID";

            DialoguePatches.DeinitializePrefix();

            Assert.AreEqual(1, playback.StopCalls);
            Assert.IsNull(DialoguePatches.CurrentEmitter);
        }
    }
}
