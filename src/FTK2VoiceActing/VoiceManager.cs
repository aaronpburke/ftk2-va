using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace FTK2VoiceActing
{
    public interface IVoicePlayback
    {
        void PlayVoiceClip(string npcId, string dialogueKey);
        void StopCurrentClip();
    }

    /// <summary>
    /// Manages discovery, loading, and playback of voice audio clips.
    /// Voice files are organized as: VoiceAssets/{NPC_ID}/{DIALOGUE_KEY}.ogg|wav
    /// </summary>
    public class VoiceManager : IVoicePlayback
    {
        private static readonly string[] SupportedExtensions = { ".ogg", ".wav" };

        private readonly string _voiceAssetsPath;
        private readonly ManualLogSource _logger;
        private readonly VoiceConfig _config;

        // Maps (npcId, dialogueKey) -> file path
        private readonly Dictionary<(string npcId, string dialogueKey), string> _voiceFileIndex;

        // Tracks keys we've already warned about to avoid log spam
        private readonly HashSet<(string, string)> _warnedMissingKeys;

        private readonly AudioPlaybackHandle _handle;

        public VoiceManager(string voiceAssetsPath, ManualLogSource logger, VoiceConfig config)
            : this(voiceAssetsPath, logger, config, new AudioPlaybackHandle())
        {
        }

        internal VoiceManager(string voiceAssetsPath, ManualLogSource logger, VoiceConfig config, AudioPlaybackHandle handle)
        {
            _voiceAssetsPath = voiceAssetsPath ?? throw new ArgumentNullException(nameof(voiceAssetsPath));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            _voiceFileIndex = new Dictionary<(string, string), string>(StringTupleComparer.OrdinalIgnoreCase);
            _warnedMissingKeys = new HashSet<(string, string)>(StringTupleComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Scans the VoiceAssets directory and builds the file lookup index.
        /// </summary>
        public void Initialize()
        {
            _voiceFileIndex.Clear();
            _warnedMissingKeys.Clear();
            _handle.Create();

            if (!Directory.Exists(_voiceAssetsPath))
            {
                _logger.LogWarning($"VoiceAssets directory not found: {_voiceAssetsPath}");
                return;
            }

            ScanDirectory(_voiceAssetsPath);

            _logger.LogInfo($"Indexed {_voiceFileIndex.Count} voice files from {_voiceAssetsPath}");
        }

        /// <summary>
        /// Scans a directory for voice files organized as {NPC_ID}/{KEY}.ext
        /// </summary>
        internal void ScanDirectory(string basePath)
        {
            if (!Directory.Exists(basePath))
                return;

            foreach (string npcDir in Directory.GetDirectories(basePath))
            {
                string npcId = Path.GetFileName(npcDir);

                foreach (string filePath in GetSupportedFilesInPriorityOrder(npcDir))
                {
                    string dialogueKey = Path.GetFileNameWithoutExtension(filePath);
                    var key = (npcId, dialogueKey);

                    if (_voiceFileIndex.ContainsKey(key))
                    {
                        if (_config.DebugLogging.Value)
                            _logger.LogDebug($"Duplicate voice file for {npcId}/{dialogueKey}, keeping first found");
                        continue;
                    }

                    _voiceFileIndex[key] = filePath;

                    if (_config.DebugLogging.Value)
                        _logger.LogDebug($"Indexed: {npcId}/{dialogueKey} -> {filePath}");
                }
            }
        }

        /// <summary>
        /// Checks whether a voice clip exists for the given NPC and dialogue key.
        /// </summary>
        public bool HasVoiceClip(string npcId, string dialogueKey)
        {
            if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(dialogueKey))
                return false;

            return _voiceFileIndex.ContainsKey((npcId, dialogueKey));
        }

        /// <summary>
        /// Gets the file path for a voice clip, or null if not found.
        /// </summary>
        public string GetVoiceClipPath(string npcId, string dialogueKey)
        {
            if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(dialogueKey))
                return null;

            if (_voiceFileIndex.TryGetValue((npcId, dialogueKey), out string path))
                return path;

            return null;
        }

        /// <summary>
        /// Plays a voice clip for the given NPC and dialogue key.
        /// Stops any currently playing clip first.
        /// </summary>
        public void PlayVoiceClip(string npcId, string dialogueKey)
        {
            if (!_config.Enabled.Value)
            {
                // Disabling voice acting should immediately silence current audio
                // and invalidate any in-flight async load that began while enabled.
                StopCurrentClip();
                return;
            }

            StopCurrentClip();

            string filePath = GetVoiceClipPath(npcId, dialogueKey);
            if (filePath == null)
            {
                if (!_warnedMissingKeys.Contains((npcId, dialogueKey)))
                {
                    _warnedMissingKeys.Add((npcId, dialogueKey));
                    _logger.LogDebug($"No voice clip for {npcId}/{dialogueKey}");
                }
                return;
            }

            _logger.LogInfo($"Loading voice clip: {npcId}/{dialogueKey} -> {filePath}");
            LoadAndPlayClip(filePath, _handle.Generation);
        }

        /// <summary>
        /// Stops the currently playing voice clip, if any.
        /// Also cleans up the previous AudioClip to prevent memory leaks.
        /// </summary>
        public void StopCurrentClip()
        {
            _handle.Stop();
        }

        /// <summary>
        /// Returns true if a voice clip is currently playing.
        /// </summary>
        public bool IsPlaying()
        {
            return _handle.IsPlaying;
        }

        /// <summary>
        /// Cleans up the managed AudioSource and GameObject.
        /// </summary>
        public void Destroy()
        {
            _handle.Destroy();
        }

        /// <summary>
        /// Returns the number of indexed voice files.
        /// </summary>
        public int IndexedFileCount => _voiceFileIndex.Count;

        /// <summary>
        /// Returns all indexed NPC IDs that have voice files.
        /// </summary>
        public IEnumerable<string> GetIndexedNpcIds()
        {
            return _voiceFileIndex.Keys.Select(k => k.npcId).Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private void LoadAndPlayClip(string filePath, int playbackGeneration)
        {
            if (!_handle.IsReady)
            {
                _logger.LogError("AudioSource not initialized. Call Initialize() first.");
                return;
            }

            AudioType audioType = GetAudioType(filePath);
            string fileUri = "file:///" + filePath.Replace('\\', '/');

            var request = UnityWebRequestMultimedia.GetAudioClip(fileUri, audioType);
            var operation = request.SendWebRequest();

            operation.completed += _ =>
            {
                if (!_handle.IsCurrentGeneration(playbackGeneration))
                {
                    request.Dispose();
                    return;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    _logger.LogError($"Failed to load voice clip: {filePath} - {request.error}");
                    request.Dispose();
                    return;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip == null)
                {
                    _logger.LogError($"Loaded null AudioClip from: {filePath}");
                    request.Dispose();
                    return;
                }

                clip.name = Path.GetFileNameWithoutExtension(filePath);
                if (!_handle.Play(clip, _config.GetVolume(), playbackGeneration))
                {
                    // Generation changed while we were processing — destroy the orphan clip
                    UnityEngine.Object.Destroy(clip);
                    request.Dispose();
                    return;
                }

                if (_config.DebugLogging.Value)
                    _logger.LogDebug($"Playing voice clip: {filePath} (volume: {_config.GetVolume():F2})");

                request.Dispose();
            };
        }

        private static IEnumerable<string> GetSupportedFilesInPriorityOrder(string npcDir)
        {
            return Directory.GetFiles(npcDir)
                .Where(filePath => IsSupportedExtension(Path.GetExtension(filePath)))
                .OrderBy(filePath => GetExtensionPriority(Path.GetExtension(filePath)))
                .ThenBy(filePath => filePath, StringComparer.OrdinalIgnoreCase);
        }

        private static int GetExtensionPriority(string extension)
        {
            for (int i = 0; i < SupportedExtensions.Length; i++)
            {
                if (string.Equals(extension, SupportedExtensions[i], StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return SupportedExtensions.Length;
        }

        internal static bool IsSupportedExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;

            foreach (string supported in SupportedExtensions)
            {
                if (string.Equals(extension, supported, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        internal static AudioType GetAudioType(string filePath)
        {
            string ext = Path.GetExtension(filePath);
            if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase))
                return AudioType.OGGVORBIS;
            if (string.Equals(ext, ".wav", StringComparison.OrdinalIgnoreCase))
                return AudioType.WAV;

            return AudioType.UNKNOWN;
        }
    }

    /// <summary>
    /// Case-insensitive comparer for (string, string) tuples.
    /// </summary>
    internal class StringTupleComparer : IEqualityComparer<(string, string)>
    {
        public static readonly StringTupleComparer OrdinalIgnoreCase = new StringTupleComparer();

        public bool Equals((string, string) x, (string, string) y)
        {
            return string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string, string) obj)
        {
            int h1 = obj.Item1 != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1) : 0;
            int h2 = obj.Item2 != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2) : 0;
            return h1 ^ (h2 * 397);
        }
    }
}
