using System.Runtime.CompilerServices;
using NUnit.Framework;
using UnityEngine;

namespace FTK2VoiceActing.Tests
{
    /// <summary>
    /// Subclass of AudioPlaybackHandle that overrides Unity API calls
    /// to avoid ECall/SecurityException in the test runner.
    /// </summary>
    internal class TestableAudioPlaybackHandle : AudioPlaybackHandle
    {
        public bool UnityObjectsCreated { get; private set; }
        public bool UnityObjectsDestroyed { get; private set; }
        public int StopUnityPlaybackCalls { get; private set; }
        public int PlayUnityClipCalls { get; private set; }
        public bool SimulateIsPlaying { get; set; }
        public bool SimulateUnityObjectDestroyed { get; set; }
        public int CreateCallCount { get; private set; }

        protected override void CreateUnityObjects()
        {
            UnityObjectsCreated = true;
            CreateCallCount++;
        }

        protected override void DestroyUnityObjects()
        {
            UnityObjectsDestroyed = true;
        }

        protected override void StopUnityPlayback()
        {
            StopUnityPlaybackCalls++;
            SimulateIsPlaying = false;
        }

        protected override void PlayUnityClip(UnityEngine.AudioClip clip, float volume)
        {
            PlayUnityClipCalls++;
            SimulateIsPlaying = true;
        }

        protected override bool GetIsPlaying()
        {
            return SimulateIsPlaying;
        }

        protected override bool IsUnityObjectDestroyed()
        {
            return SimulateUnityObjectDestroyed;
        }
    }

    [TestFixture]
    public class AudioPlaybackHandleTests
    {
        // --- Initial state ---

        [Test]
        public void NewHandle_IsNotReady()
        {
            var handle = new TestableAudioPlaybackHandle();
            Assert.IsFalse(handle.IsReady);
        }

        [Test]
        public void NewHandle_GenerationIsZero()
        {
            var handle = new TestableAudioPlaybackHandle();
            Assert.AreEqual(0, handle.Generation);
        }

        // --- Create ---

        [Test]
        public void Create_SetsIsReady()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            Assert.IsTrue(handle.IsReady);
        }

        [Test]
        public void Create_CallsCreateUnityObjects()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            Assert.IsTrue(handle.UnityObjectsCreated);
        }

        [Test]
        public void Create_IsIdempotent()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            handle.Create(); // second call should be safe
            Assert.IsTrue(handle.IsReady);
        }

        // --- Stop and generation ---

        [Test]
        public void Stop_IncrementsGeneration()
        {
            var handle = new TestableAudioPlaybackHandle();
            Assert.AreEqual(0, handle.Generation);

            handle.Stop();
            Assert.AreEqual(1, handle.Generation);

            handle.Stop();
            Assert.AreEqual(2, handle.Generation);
        }

        [Test]
        public void Stop_ReturnsNewGeneration()
        {
            var handle = new TestableAudioPlaybackHandle();
            int gen = handle.Stop();
            Assert.AreEqual(1, gen);
            Assert.AreEqual(gen, handle.Generation);
        }

        [Test]
        public void Stop_IncrementsGeneration_EvenWhenNotReady()
        {
            var handle = new TestableAudioPlaybackHandle();
            // Not created, but Stop should still increment generation
            int gen = handle.Stop();
            Assert.AreEqual(1, gen);
        }

        [Test]
        public void Stop_CallsStopUnityPlayback_WhenCreated()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            handle.Stop();
            Assert.AreEqual(1, handle.StopUnityPlaybackCalls);
        }

        [Test]
        public void Stop_DoesNotCallStopUnityPlayback_WhenNotCreated()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Stop();
            Assert.AreEqual(0, handle.StopUnityPlaybackCalls);
        }

        // --- IsCurrentGeneration ---

        [Test]
        public void IsCurrentGeneration_TrueForCurrentGen_WhenReady()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            Assert.IsTrue(handle.IsCurrentGeneration(0));
        }

        [Test]
        public void IsCurrentGeneration_FalseForOldGen()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            int oldGen = handle.Generation;
            handle.Stop();
            Assert.IsFalse(handle.IsCurrentGeneration(oldGen));
        }

        [Test]
        public void IsCurrentGeneration_FalseWhenNotReady()
        {
            var handle = new TestableAudioPlaybackHandle();
            // Not created — even generation 0 should be false
            Assert.IsFalse(handle.IsCurrentGeneration(0));
        }

        [Test]
        public void IsCurrentGeneration_TrueAfterStopForNewGen()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            int newGen = handle.Stop();
            Assert.IsTrue(handle.IsCurrentGeneration(newGen));
        }

        // --- Play ---

        /// <summary>
        /// Creates an AudioClip reference without invoking its constructor,
        /// bypassing Unity native code that would crash in the test runner.
        /// The returned object has all fields zeroed — do not access its members.
        /// </summary>
        private static AudioClip CreateUninitializedAudioClip()
        {
            return (AudioClip)RuntimeHelpers.GetUninitializedObject(typeof(AudioClip));
        }

        [Test]
        public void Play_AutoCreates_WhenNotReady()
        {
            var handle = new TestableAudioPlaybackHandle();
            bool result = handle.Play(CreateUninitializedAudioClip(), 1.0f, 0);
            // Play now auto-creates Unity objects if they don't exist
            Assert.IsTrue(result);
            Assert.AreEqual(1, handle.PlayUnityClipCalls);
            Assert.IsTrue(handle.UnityObjectsCreated);
        }

        [Test]
        public void Play_ReturnsFalse_WhenClipIsNull()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();

            bool result = handle.Play(null, 1.0f, handle.Generation);

            Assert.IsFalse(result);
            Assert.AreEqual(0, handle.PlayUnityClipCalls);
        }

        [Test]
        public void Play_ReturnsFalse_WhenGenerationMismatch()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            handle.Stop(); // generation is now 1
            // Try to play with stale generation 0
            bool result = handle.Play(CreateUninitializedAudioClip(), 1.0f, 0);
            Assert.IsFalse(result);
            Assert.AreEqual(0, handle.PlayUnityClipCalls);
        }

        [Test]
        public void Play_ReturnsTrue_WhenGenerationMatches()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            int gen = handle.Generation;
            bool result = handle.Play(CreateUninitializedAudioClip(), 0.8f, gen);
            Assert.IsTrue(result);
            Assert.AreEqual(1, handle.PlayUnityClipCalls);
        }

        [Test]
        public void Play_ReturnsTrue_AfterStopWithNewGen()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            int newGen = handle.Stop();
            bool result = handle.Play(CreateUninitializedAudioClip(), 1.0f, newGen);
            Assert.IsTrue(result);
            Assert.AreEqual(1, handle.PlayUnityClipCalls);
        }

        // --- Destroy ---

        [Test]
        public void Destroy_SetsNotReady()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            Assert.IsTrue(handle.IsReady);

            handle.Destroy();
            Assert.IsFalse(handle.IsReady);
        }

        [Test]
        public void Destroy_CallsDestroyUnityObjects()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            handle.Destroy();
            Assert.IsTrue(handle.UnityObjectsDestroyed);
        }

        [Test]
        public void Destroy_IncrementsGeneration()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            int genBefore = handle.Generation;
            handle.Destroy();
            Assert.Greater(handle.Generation, genBefore);
        }

        [Test]
        public void Destroy_InvalidatesCurrentGeneration()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            int gen = handle.Generation;
            handle.Destroy();
            // After destroy, IsCurrentGeneration should be false (not ready)
            Assert.IsFalse(handle.IsCurrentGeneration(gen));
        }

        [Test]
        public void Destroy_IsIdempotent()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            handle.Destroy();
            // Second destroy should not throw
            Assert.DoesNotThrow(() => handle.Destroy());
            Assert.IsFalse(handle.IsReady);
        }

        [Test]
        public void Destroy_WhenNeverCreated_DoesNotThrow()
        {
            var handle = new TestableAudioPlaybackHandle();
            Assert.DoesNotThrow(() => handle.Destroy());
        }

        // --- Generation sequence across lifecycle ---

        [Test]
        public void GenerationSequence_AcrossFullLifecycle()
        {
            var handle = new TestableAudioPlaybackHandle();
            Assert.AreEqual(0, handle.Generation);

            handle.Create();
            Assert.AreEqual(0, handle.Generation); // Create doesn't change generation

            handle.Stop(); // gen = 1
            Assert.AreEqual(1, handle.Generation);

            handle.Stop(); // gen = 2
            Assert.AreEqual(2, handle.Generation);

            handle.Destroy(); // gen = 3 (Stop inside Destroy)
            Assert.AreEqual(3, handle.Generation);
        }

        [Test]
        public void StaleGeneration_InvalidatedByStop()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();

            // Simulate: capture generation before play request
            int requestGen = handle.Generation;
            Assert.IsTrue(handle.IsCurrentGeneration(requestGen));

            // Another Stop happens (e.g., new dialogue line)
            handle.Stop();

            // The original request generation is now stale
            Assert.IsFalse(handle.IsCurrentGeneration(requestGen));
        }

        // --- Auto-recreation after Unity destroy ---

        [Test]
        public void Play_RecreatesUnityObjects_WhenDestroyedBySceneTransition()
        {
            var handle = new RecreatingTestHandle();
            handle.Create();
            Assert.AreEqual(1, handle.CreateCallCount);

            // Simulate Unity destroying objects during scene transition, then recovery on recreate
            handle.SimulateDestroyedOnce = true;
            bool result = handle.Play(CreateUninitializedAudioClip(), 1.0f, handle.Generation);
            Assert.IsTrue(result);
            Assert.AreEqual(2, handle.CreateCallCount); // Recreated
            Assert.AreEqual(1, handle.PlayUnityClipCalls);
        }

        [Test]
        public void IsReady_ReturnsFalse_WhenUnityObjectsDestroyed()
        {
            var handle = new TestableAudioPlaybackHandle();
            handle.Create();
            Assert.IsTrue(handle.IsReady);

            handle.SimulateUnityObjectDestroyed = true;
            Assert.IsFalse(handle.IsReady);
        }
    }

    /// <summary>
    /// Test handle that simulates Unity destroying objects once, then recovering
    /// after recreation (mimicking a scene transition scenario).
    /// </summary>
    internal class RecreatingTestHandle : TestableAudioPlaybackHandle
    {
        public bool SimulateDestroyedOnce { get; set; }
        private int _createCount;

        protected override bool IsUnityObjectDestroyed()
        {
            // Only report destroyed when SimulateDestroyedOnce is true and
            // we haven't recreated since it was set
            if (SimulateDestroyedOnce && _createCount <= 1)
                return true;
            return false;
        }

        protected override void CreateUnityObjects()
        {
            base.CreateUnityObjects();
            _createCount++;
        }
    }
}
