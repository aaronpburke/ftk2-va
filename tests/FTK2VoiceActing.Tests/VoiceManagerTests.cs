using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace FTK2VoiceActing.Tests
{
    [TestFixture]
    public class VoiceManagerTests
    {
        private string _testDir;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "FTK2VA_Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        private string CreateVoiceAssetsDir()
        {
            string vaDir = Path.Combine(_testDir, "VoiceAssets");
            Directory.CreateDirectory(vaDir);
            return vaDir;
        }

        private void CreateTestFile(string basePath, string npcId, string dialogueKey, string extension)
        {
            string npcDir = Path.Combine(basePath, npcId);
            Directory.CreateDirectory(npcDir);
            string filePath = Path.Combine(npcDir, dialogueKey + extension);
            File.WriteAllText(filePath, "fake audio data");
        }

        // --- IsSupportedExtension tests ---

        [TestCase(".ogg", true)]
        [TestCase(".wav", true)]
        [TestCase(".OGG", true)]
        [TestCase(".WAV", true)]
        [TestCase(".Ogg", true)]
        [TestCase(".mp3", false)]
        [TestCase(".flac", false)]
        [TestCase(".txt", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsSupportedExtension_ReturnsExpected(string ext, bool expected)
        {
            Assert.AreEqual(expected, VoiceManager.IsSupportedExtension(ext));
        }

        // --- GetAudioType tests ---

        [Test]
        public void GetAudioType_OggFile_ReturnsOggVorbis()
        {
            var result = VoiceManager.GetAudioType("test.ogg");
            Assert.AreEqual(UnityEngine.AudioType.OGGVORBIS, result);
        }

        [Test]
        public void GetAudioType_WavFile_ReturnsWav()
        {
            var result = VoiceManager.GetAudioType("test.wav");
            Assert.AreEqual(UnityEngine.AudioType.WAV, result);
        }

        [Test]
        public void GetAudioType_UnknownExt_ReturnsUnknown()
        {
            var result = VoiceManager.GetAudioType("test.mp3");
            Assert.AreEqual(UnityEngine.AudioType.UNKNOWN, result);
        }

        // --- File discovery and path resolution tests ---

        [Test]
        public void ScanDirectory_FindsOggFiles()
        {
            string vaDir = CreateVoiceAssetsDir();
            CreateTestFile(vaDir, "NPC_BARMAID", "STORY_1_1_DIALOG_1", ".ogg");

            var manager = CreateTestManager(vaDir);
            manager.ScanDirectory(vaDir);

            Assert.IsTrue(manager.HasVoiceClip("NPC_BARMAID", "STORY_1_1_DIALOG_1"));
            Assert.AreEqual(1, manager.IndexedFileCount);
        }

        [Test]
        public void ScanDirectory_FindsWavFiles()
        {
            string vaDir = CreateVoiceAssetsDir();
            CreateTestFile(vaDir, "NPC_BARMAID", "STORY_1_1_DIALOG_1", ".wav");

            var manager = CreateTestManager(vaDir);
            manager.ScanDirectory(vaDir);

            Assert.IsTrue(manager.HasVoiceClip("NPC_BARMAID", "STORY_1_1_DIALOG_1"));
        }

        [Test]
        public void ScanDirectory_IgnoresUnsupportedFormats()
        {
            string vaDir = CreateVoiceAssetsDir();
            CreateTestFile(vaDir, "NPC_BARMAID", "STORY_1_1_DIALOG_1", ".mp3");
            CreateTestFile(vaDir, "NPC_BARMAID", "STORY_1_1_DIALOG_2", ".txt");

            var manager = CreateTestManager(vaDir);
            manager.ScanDirectory(vaDir);

            Assert.AreEqual(0, manager.IndexedFileCount);
        }

        [Test]
        public void ScanDirectory_MultipleNpcs()
        {
            string vaDir = CreateVoiceAssetsDir();
            CreateTestFile(vaDir, "NPC_BARMAID", "DIALOG_1", ".ogg");
            CreateTestFile(vaDir, "NPC_HILDEBRANT", "DIALOG_2", ".wav");
            CreateTestFile(vaDir, "NARRATOR", "STORY_1_1_INTRO", ".ogg");

            var manager = CreateTestManager(vaDir);
            manager.ScanDirectory(vaDir);

            Assert.AreEqual(3, manager.IndexedFileCount);
            Assert.IsTrue(manager.HasVoiceClip("NPC_BARMAID", "DIALOG_1"));
            Assert.IsTrue(manager.HasVoiceClip("NPC_HILDEBRANT", "DIALOG_2"));
            Assert.IsTrue(manager.HasVoiceClip("NARRATOR", "STORY_1_1_INTRO"));
        }

        [Test]
        public void ScanDirectory_DuplicateKeepsFirst()
        {
            string vaDir = CreateVoiceAssetsDir();
            CreateTestFile(vaDir, "NPC_BARMAID", "DIALOG_1", ".ogg");
            CreateTestFile(vaDir, "NPC_BARMAID", "DIALOG_1", ".wav");

            var manager = CreateTestManager(vaDir);
            manager.ScanDirectory(vaDir);

            // Should index a single file for the duplicate key.
            Assert.AreEqual(1, manager.IndexedFileCount);
            Assert.IsTrue(manager.HasVoiceClip("NPC_BARMAID", "DIALOG_1"));
        }

        [Test]
        public void ScanDirectory_DuplicatePrefersOggOverWav()
        {
            string vaDir = CreateVoiceAssetsDir();
            CreateTestFile(vaDir, "NPC_BARMAID", "DIALOG_1", ".wav");
            CreateTestFile(vaDir, "NPC_BARMAID", "DIALOG_1", ".ogg");

            var manager = CreateTestManager(vaDir);
            manager.ScanDirectory(vaDir);

            string path = manager.GetVoiceClipPath("NPC_BARMAID", "DIALOG_1");
            Assert.IsNotNull(path);
            Assert.IsTrue(path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void ScanDirectory_CaseInsensitiveLookup()
        {
            string vaDir = CreateVoiceAssetsDir();
            CreateTestFile(vaDir, "NPC_BARMAID", "STORY_1_1_DIALOG_1", ".ogg");

            var manager = CreateTestManager(vaDir);
            manager.ScanDirectory(vaDir);

            // Case-insensitive lookup should work
            Assert.IsTrue(manager.HasVoiceClip("npc_barmaid", "story_1_1_dialog_1"));
            Assert.IsTrue(manager.HasVoiceClip("NPC_BARMAID", "STORY_1_1_DIALOG_1"));
        }

        [Test]
        public void ScanDirectory_EmptyDirectory_NoFiles()
        {
            string vaDir = CreateVoiceAssetsDir();
            Directory.CreateDirectory(Path.Combine(vaDir, "NPC_EMPTY"));

            var manager = CreateTestManager(vaDir);
            manager.ScanDirectory(vaDir);

            Assert.AreEqual(0, manager.IndexedFileCount);
        }

        [Test]
        public void ScanDirectory_NonexistentPath_NoError()
        {
            string vaDir = Path.Combine(_testDir, "NonExistent");

            var manager = CreateTestManager(vaDir);
            manager.ScanDirectory(vaDir);

            Assert.AreEqual(0, manager.IndexedFileCount);
        }

        [Test]
        public void ScanDirectory_FilesInRootIgnored()
        {
            // Files directly in VoiceAssets/ (not in an NPC subfolder) should be ignored
            string vaDir = CreateVoiceAssetsDir();
            File.WriteAllText(Path.Combine(vaDir, "stray_file.ogg"), "data");

            var manager = CreateTestManager(vaDir);
            manager.ScanDirectory(vaDir);

            Assert.AreEqual(0, manager.IndexedFileCount);
        }

        // --- GetVoiceClipPath tests ---

        [Test]
        public void GetVoiceClipPath_ExistingFile_ReturnsPath()
        {
            string vaDir = CreateVoiceAssetsDir();
            CreateTestFile(vaDir, "NPC_BARMAID", "DIALOG_1", ".ogg");

            var manager = CreateTestManager(vaDir);
            manager.ScanDirectory(vaDir);

            string path = manager.GetVoiceClipPath("NPC_BARMAID", "DIALOG_1");
            Assert.IsNotNull(path);
            Assert.IsTrue(path.EndsWith(".ogg"));
        }

        [Test]
        public void GetVoiceClipPath_MissingFile_ReturnsNull()
        {
            string vaDir = CreateVoiceAssetsDir();
            var manager = CreateTestManager(vaDir);
            manager.ScanDirectory(vaDir);

            Assert.IsNull(manager.GetVoiceClipPath("NPC_BARMAID", "NONEXISTENT"));
        }

        [Test]
        public void GetVoiceClipPath_NullNpcId_ReturnsNull()
        {
            string vaDir = CreateVoiceAssetsDir();
            var manager = CreateTestManager(vaDir);

            Assert.IsNull(manager.GetVoiceClipPath(null, "DIALOG_1"));
        }

        [Test]
        public void GetVoiceClipPath_NullDialogueKey_ReturnsNull()
        {
            string vaDir = CreateVoiceAssetsDir();
            var manager = CreateTestManager(vaDir);

            Assert.IsNull(manager.GetVoiceClipPath("NPC_BARMAID", null));
        }

        [Test]
        public void GetVoiceClipPath_EmptyNpcId_ReturnsNull()
        {
            string vaDir = CreateVoiceAssetsDir();
            var manager = CreateTestManager(vaDir);

            Assert.IsNull(manager.GetVoiceClipPath("", "DIALOG_1"));
        }

        [Test]
        public void GetVoiceClipPath_EmptyDialogueKey_ReturnsNull()
        {
            string vaDir = CreateVoiceAssetsDir();
            var manager = CreateTestManager(vaDir);

            Assert.IsNull(manager.GetVoiceClipPath("NPC_BARMAID", ""));
        }

        // --- HasVoiceClip tests ---

        [Test]
        public void HasVoiceClip_NullInputs_ReturnsFalse()
        {
            string vaDir = CreateVoiceAssetsDir();
            var manager = CreateTestManager(vaDir);

            Assert.IsFalse(manager.HasVoiceClip(null, null));
            Assert.IsFalse(manager.HasVoiceClip(null, "DIALOG_1"));
            Assert.IsFalse(manager.HasVoiceClip("NPC_BARMAID", null));
        }

        [Test]
        public void HasVoiceClip_EmptyInputs_ReturnsFalse()
        {
            string vaDir = CreateVoiceAssetsDir();
            var manager = CreateTestManager(vaDir);

            Assert.IsFalse(manager.HasVoiceClip("", ""));
            Assert.IsFalse(manager.HasVoiceClip("", "DIALOG_1"));
            Assert.IsFalse(manager.HasVoiceClip("NPC_BARMAID", ""));
        }

        // --- GetIndexedNpcIds tests ---

        [Test]
        public void GetIndexedNpcIds_ReturnsDistinctIds()
        {
            string vaDir = CreateVoiceAssetsDir();
            CreateTestFile(vaDir, "NPC_BARMAID", "DIALOG_1", ".ogg");
            CreateTestFile(vaDir, "NPC_BARMAID", "DIALOG_2", ".ogg");
            CreateTestFile(vaDir, "NARRATOR", "STORY_1_1_INTRO", ".ogg");

            var manager = CreateTestManager(vaDir);
            manager.ScanDirectory(vaDir);

            var npcIds = new List<string>(manager.GetIndexedNpcIds());
            Assert.AreEqual(2, npcIds.Count);
            Assert.Contains("NPC_BARMAID", npcIds);
            Assert.Contains("NARRATOR", npcIds);
        }

        // --- Constructor validation ---

        [Test]
        public void Constructor_NullPath_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new VoiceManager(null, new TestLogSource(), CreateTestConfig()));
        }

        [Test]
        public void Constructor_NullLogger_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new VoiceManager(_testDir, null, CreateTestConfig()));
        }

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new VoiceManager(_testDir, new TestLogSource(), null));
        }

        [Test]
        public void PlayVoiceClip_WhenDisabled_StopsCurrentPlaybackToInvalidatePendingLoads()
        {
            string vaDir = CreateVoiceAssetsDir();
            var config = CreateTestConfig();
            var handle = new TestableAudioPlaybackHandle();
            var manager = new VoiceManager(vaDir, new TestLogSource(), config, handle);
            manager.Initialize();
            config.Enabled.Value = false;

            manager.PlayVoiceClip("NPC_BARMAID", "DIALOG_1");

            Assert.AreEqual(1, handle.Generation);
            Assert.AreEqual(1, handle.StopUnityPlaybackCalls);
        }

        [Test]
        public void PlayVoiceClip_DisableThenReEnable_ResumesNormalBehavior()
        {
            string vaDir = CreateVoiceAssetsDir();
            var config = CreateTestConfig();
            var handle = new TestableAudioPlaybackHandle();
            var manager = new VoiceManager(vaDir, new TestLogSource(), config, handle);
            manager.Initialize();

            // Disable — should stop and increment generation
            config.Enabled.Value = false;
            manager.PlayVoiceClip("NPC_BARMAID", "DIALOG_1");
            Assert.AreEqual(1, handle.Generation);
            Assert.AreEqual(1, handle.StopUnityPlaybackCalls);

            // Re-enable — should stop again (normal pre-play stop) then proceed
            // No voice file exists so LoadAndPlayClip is not reached, but the
            // stop/generation behavior is exercised.
            config.Enabled.Value = true;
            manager.PlayVoiceClip("NPC_BARMAID", "DIALOG_1");
            // Generation incremented again by the normal StopCurrentClip before play
            Assert.AreEqual(2, handle.Generation);
            Assert.AreEqual(2, handle.StopUnityPlaybackCalls);
        }

        // --- Deeply nested wrong structure ---

        [Test]
        public void ScanDirectory_DeeplyNestedFiles_Ignored()
        {
            // Files nested deeper than NPC/{file} should not be indexed
            string vaDir = CreateVoiceAssetsDir();
            string deepDir = Path.Combine(vaDir, "NPC_BARMAID", "SubFolder");
            Directory.CreateDirectory(deepDir);
            File.WriteAllText(Path.Combine(deepDir, "DIALOG_1.ogg"), "data");

            var manager = CreateTestManager(vaDir);
            manager.ScanDirectory(vaDir);

            // The file is under NPC_BARMAID/SubFolder/ — ScanDirectory only scans direct children
            Assert.IsFalse(manager.HasVoiceClip("NPC_BARMAID", "DIALOG_1"));
        }

        // --- StringTupleComparer tests ---

        [Test]
        public void StringTupleComparer_CaseInsensitiveEquals()
        {
            var comparer = StringTupleComparer.OrdinalIgnoreCase;
            Assert.IsTrue(comparer.Equals(("ABC", "DEF"), ("abc", "def")));
            Assert.IsTrue(comparer.Equals(("abc", "def"), ("ABC", "DEF")));
            Assert.IsFalse(comparer.Equals(("ABC", "DEF"), ("ABC", "XYZ")));
            Assert.IsFalse(comparer.Equals(("ABC", "DEF"), ("XYZ", "DEF")));
        }

        [Test]
        public void StringTupleComparer_CaseInsensitiveHashCode()
        {
            var comparer = StringTupleComparer.OrdinalIgnoreCase;
            int h1 = comparer.GetHashCode(("ABC", "DEF"));
            int h2 = comparer.GetHashCode(("abc", "def"));
            Assert.AreEqual(h1, h2);
        }

        [Test]
        public void StringTupleComparer_NullHandling()
        {
            var comparer = StringTupleComparer.OrdinalIgnoreCase;
            Assert.IsTrue(comparer.Equals(((string)null, (string)null), ((string)null, (string)null)));
            Assert.IsFalse(comparer.Equals(((string)null, "A"), ("B", (string)null)));
            Assert.DoesNotThrow(() => comparer.GetHashCode(((string)null, (string)null)));
            Assert.DoesNotThrow(() => comparer.GetHashCode(((string)null, "A")));
        }

        // --- Helpers ---

        private VoiceManager CreateTestManager(string vaDir)
        {
            return new VoiceManager(vaDir, new TestLogSource(), CreateTestConfig());
        }

        private VoiceConfig CreateTestConfig()
        {
            // VoiceConfig requires a BepInEx ConfigFile, which we can't easily create in tests.
            // We use a stub config file backed by a temp file.
            string cfgPath = Path.Combine(_testDir, "test_config.cfg");
            File.WriteAllText(cfgPath, "");
            var configFile = new BepInEx.Configuration.ConfigFile(cfgPath, false);
            return new VoiceConfig(configFile);
        }
    }

    /// <summary>
    /// Minimal test logger that does nothing. Used in tests where we can't
    /// create a real BepInEx ManualLogSource.
    /// </summary>
    internal class TestLogSource : BepInEx.Logging.ManualLogSource
    {
        public TestLogSource() : base("Test") { }
    }
}
