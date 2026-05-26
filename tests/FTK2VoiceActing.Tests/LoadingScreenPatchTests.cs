using NUnit.Framework;

namespace FTK2VoiceActing.Tests
{
    [TestFixture]
    public class LoadingScreenPatchTests
    {
        [SetUp]
        public void SetUp()
        {
            LoadingScreenPatches.VoiceManager = null;
        }

        // --- GetNarratorKey tests ---

        [Test]
        public void GetNarratorKey_AppendsIntro()
        {
            Assert.AreEqual("STORY_1_1_INTRO", LoadingScreenPatches.GetNarratorKey("STORY_1_1"));
        }

        [Test]
        public void GetNarratorKey_SideAdventure()
        {
            Assert.AreEqual("SIDE_ADVENTURE_DARK_CARNIVAL_INTRO",
                LoadingScreenPatches.GetNarratorKey("SIDE_ADVENTURE_DARK_CARNIVAL"));
        }

        [Test]
        public void GetNarratorKey_Null_ReturnsNull()
        {
            Assert.IsNull(LoadingScreenPatches.GetNarratorKey(null));
        }

        [Test]
        public void GetNarratorKey_Empty_ReturnsNull()
        {
            Assert.IsNull(LoadingScreenPatches.GetNarratorKey(""));
        }

        // --- NarratorNpcId ---

        [Test]
        public void NarratorNpcId_IsNARRATOR()
        {
            Assert.AreEqual("NARRATOR", LoadingScreenPatches.NarratorNpcId);
        }

        // --- InitializePostfix null safety ---

        [Test]
        public void InitializePostfix_NullVoiceManager_DoesNotThrow()
        {
            LoadingScreenPatches.VoiceManager = null;
            Assert.DoesNotThrow(() => LoadingScreenPatches.InitializePostfix("STORY_1_1"));
        }

        [Test]
        public void InitializePostfix_PlaysNarratorVoice()
        {
            var playback = new FakeVoicePlayback();
            LoadingScreenPatches.VoiceManager = playback;

            LoadingScreenPatches.InitializePostfix("STORY_1_1");

            Assert.AreEqual(1, playback.PlayCalls);
            Assert.AreEqual("NARRATOR", playback.LastNpcId);
            Assert.AreEqual("STORY_1_1_INTRO", playback.LastDialogueKey);
        }

        [Test]
        public void InitializePostfix_NullAdventureId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => LoadingScreenPatches.InitializePostfix(null));
        }

        [Test]
        public void InitializePostfix_EmptyAdventureId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => LoadingScreenPatches.InitializePostfix(""));
        }

        // --- HidePrefix null safety ---

        [Test]
        public void HidePrefix_NullVoiceManager_DoesNotThrow()
        {
            LoadingScreenPatches.VoiceManager = null;
            Assert.DoesNotThrow(() => LoadingScreenPatches.HidePrefix());
        }

        [Test]
        public void HidePrefix_StopsVoice_WhenVoiceManagerIsSet()
        {
            var playback = new FakeVoicePlayback();
            LoadingScreenPatches.VoiceManager = playback;

            LoadingScreenPatches.HidePrefix();

            Assert.AreEqual(1, playback.StopCalls);
        }

        // --- Reset tests ---

        [Test]
        public void Reset_NullsVoiceManager()
        {
            LoadingScreenPatches.VoiceManager = new FakeVoicePlayback();
            LoadingScreenPatches.Reset();
            Assert.IsNull(LoadingScreenPatches.VoiceManager);
        }
    }
}
