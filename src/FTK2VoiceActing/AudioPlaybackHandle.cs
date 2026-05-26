using UnityEngine;

namespace FTK2VoiceActing
{
    /// <summary>
    /// Encapsulates the Unity AudioSource lifecycle, clip management, and
    /// playback generation counter. Centralizes all Unity audio API calls
    /// so the rest of the mod never touches AudioSource/GameObject directly.
    /// </summary>
    internal class AudioPlaybackHandle
    {
        private GameObject _gameObject;
        private AudioSource _source;
        private bool _created;
        private int _generation;

        /// <summary>
        /// Whether the audio source has been created and is ready for playback.
        /// </summary>
        public bool IsReady => _created;

        /// <summary>
        /// Whether audio is currently playing.
        /// </summary>
        public bool IsPlaying => _created && GetIsPlaying();

        /// <summary>
        /// The current playback generation. Incremented on every Stop/Destroy.
        /// </summary>
        public int Generation => _generation;

        /// <summary>
        /// Creates the Unity GameObject and AudioSource. Safe to call multiple times.
        /// </summary>
        public void Create()
        {
            if (_created)
                return;

            CreateUnityObjects();
            _created = true;
        }

        /// <summary>
        /// Stops any playing clip, destroys the current AudioClip, and
        /// increments the generation counter. Returns the new generation.
        /// </summary>
        public int Stop()
        {
            _generation++;

            if (_created)
                StopUnityPlayback();

            return _generation;
        }

        /// <summary>
        /// Assigns a clip and plays it at the given volume, but only if the
        /// provided generation matches the current generation (i.e., no newer
        /// Stop has been issued since this playback was requested) and the clip
        /// is valid.
        /// Returns true if playback started.
        /// </summary>
        public bool Play(AudioClip clip, float volume, int requestGeneration)
        {
            if (!_created || ReferenceEquals(clip, null) || requestGeneration != _generation)
                return false;

            PlayUnityClip(clip, volume);
            return true;
        }

        /// <summary>
        /// Returns true if the given generation matches the current generation
        /// and the handle is ready.
        /// </summary>
        public bool IsCurrentGeneration(int generation)
        {
            return _created && generation == _generation;
        }

        /// <summary>
        /// Stops playback, destroys the AudioClip, GameObject, and AudioSource.
        /// Resets all state.
        /// </summary>
        public void Destroy()
        {
            Stop();

            if (_created)
            {
                DestroyUnityObjects();
                _gameObject = null;
                _source = null;
                _created = false;
            }
        }

        /// <summary>
        /// Creates the Unity GameObject and AudioSource.
        /// Protected virtual so tests can override without invoking Unity APIs.
        /// </summary>
        protected virtual void CreateUnityObjects()
        {
            _gameObject = new GameObject("FTK2VoiceActing_AudioSource");
            Object.DontDestroyOnLoad(_gameObject);
            _source = _gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // 2D sound
        }

        /// <summary>
        /// Destroys the Unity GameObject (which also destroys the AudioSource).
        /// Protected virtual so tests can override without invoking Unity APIs.
        /// </summary>
        protected virtual void DestroyUnityObjects()
        {
            Object.Destroy(_gameObject);
        }

        /// <summary>
        /// Stops the AudioSource and destroys the current clip.
        /// Protected virtual so tests can override without invoking Unity APIs.
        /// </summary>
        protected virtual void StopUnityPlayback()
        {
            if (_source.isPlaying)
                _source.Stop();

            if (_source.clip != null)
            {
                Object.Destroy(_source.clip);
                _source.clip = null;
            }
        }

        /// <summary>
        /// Assigns a clip to the AudioSource and starts playback.
        /// Protected virtual so tests can override without invoking Unity APIs.
        /// </summary>
        protected virtual void PlayUnityClip(AudioClip clip, float volume)
        {
            _source.clip = clip;
            _source.volume = volume;
            _source.Play();
        }

        /// <summary>
        /// Returns whether the AudioSource is currently playing.
        /// Protected virtual so tests can override without invoking Unity APIs.
        /// </summary>
        protected virtual bool GetIsPlaying()
        {
            return _source.isPlaying;
        }
    }
}
