using System.IO;
using NUnit.Framework;
using BepInEx.Configuration;

namespace FTK2VoiceActing.Tests
{
    [TestFixture]
    public class VoiceConfigTests
    {
        private string _testDir;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "FTK2VA_ConfigTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        private VoiceConfig CreateConfig()
        {
            string cfgPath = Path.Combine(_testDir, "test.cfg");
            File.WriteAllText(cfgPath, "");
            var configFile = new ConfigFile(cfgPath, false);
            return new VoiceConfig(configFile);
        }

        // --- Default values ---

        [Test]
        public void Defaults_Enabled_True()
        {
            var config = CreateConfig();
            Assert.IsTrue(config.Enabled.Value);
        }

        [Test]
        public void Defaults_Volume_One()
        {
            var config = CreateConfig();
            Assert.AreEqual(1.0f, config.Volume.Value);
        }

        [Test]
        public void Defaults_DebugLogging_False()
        {
            var config = CreateConfig();
            Assert.IsFalse(config.DebugLogging.Value);
        }

        // --- GetVolume clamping ---

        [Test]
        public void GetVolume_DefaultValue_ReturnsOne()
        {
            var config = CreateConfig();
            Assert.AreEqual(1.0f, config.GetVolume());
        }

        [Test]
        public void GetVolume_SetToHalf_ReturnsHalf()
        {
            var config = CreateConfig();
            config.Volume.Value = 0.5f;
            Assert.AreEqual(0.5f, config.GetVolume());
        }

        [Test]
        public void GetVolume_SetToZero_ReturnsZero()
        {
            var config = CreateConfig();
            config.Volume.Value = 0f;
            Assert.AreEqual(0f, config.GetVolume());
        }

        [Test]
        public void GetVolume_NegativeValue_ClampedToZero()
        {
            var config = CreateConfig();
            // BepInEx AcceptableValueRange should prevent this, but GetVolume() also clamps
            // We test the clamp logic directly
            config.Volume.Value = -0.5f;
            float result = config.GetVolume();
            // AcceptableValueRange may have already clamped it, or our GetVolume clamps
            Assert.GreaterOrEqual(result, 0f);
        }

        [Test]
        public void GetVolume_OverOne_ClampedToOne()
        {
            var config = CreateConfig();
            config.Volume.Value = 2.0f;
            float result = config.GetVolume();
            Assert.LessOrEqual(result, 1f);
        }

        // --- Enable/disable toggle ---

        [Test]
        public void Enabled_CanBeToggled()
        {
            var config = CreateConfig();
            Assert.IsTrue(config.Enabled.Value);

            config.Enabled.Value = false;
            Assert.IsFalse(config.Enabled.Value);

            config.Enabled.Value = true;
            Assert.IsTrue(config.Enabled.Value);
        }

        // --- Debug logging toggle ---

        [Test]
        public void DebugLogging_CanBeToggled()
        {
            var config = CreateConfig();
            Assert.IsFalse(config.DebugLogging.Value);

            config.DebugLogging.Value = true;
            Assert.IsTrue(config.DebugLogging.Value);
        }
    }
}
