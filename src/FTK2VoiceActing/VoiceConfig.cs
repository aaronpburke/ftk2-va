using BepInEx.Configuration;

namespace FTK2VoiceActing
{
    /// <summary>
    /// Wraps BepInEx configuration entries for the voice acting mod.
    /// </summary>
    public class VoiceConfig
    {
        public ConfigEntry<bool> Enabled { get; }
        public ConfigEntry<float> Volume { get; }
        public ConfigEntry<bool> DebugLogging { get; }

        public VoiceConfig(ConfigFile config)
        {
            Enabled = config.Bind(
                "General",
                "Enabled",
                true,
                "Enable or disable voice acting playback.");

            Volume = config.Bind(
                "General",
                "Volume",
                1.0f,
                new ConfigDescription(
                    "Voice acting volume (0.0 = silent, 1.0 = full volume).",
                    new AcceptableValueRange<float>(0f, 1f)));

            DebugLogging = config.Bind(
                "General",
                "DebugLogging",
                false,
                "Enable verbose debug logging to BepInEx console.");
        }

        /// <summary>
        /// Returns the clamped volume value in [0, 1].
        /// </summary>
        public float GetVolume()
        {
            float v = Volume.Value;
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
