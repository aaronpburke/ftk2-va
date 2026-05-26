using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace FTK2VoiceActing
{
    /// <summary>
    /// BepInEx plugin that adds voice acting support to For the King 2.
    /// Voice files are loaded from BepInEx/plugins/FTK2VoiceActing/VoiceAssets/
    /// organized by NPC ID subdirectories.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("For The King II.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "dev.ftk2.voiceacting";
        public const string PluginName = "FTK2 Voice Acting";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log { get; private set; }
        internal static VoiceConfig VoiceActingConfig { get; private set; }
        internal static VoiceManager VoiceManager { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginName} v{PluginVersion} loading...");

            VoiceActingConfig = new VoiceConfig(Config);

            string pluginDir = Path.GetDirectoryName(Info.Location);
            string voiceAssetsPath = Path.Combine(pluginDir, "VoiceAssets");

            VoiceManager = new VoiceManager(voiceAssetsPath, Log, VoiceActingConfig);
            VoiceManager.Initialize();

            DialoguePatches.VoiceManager = VoiceManager;
            LoadingScreenPatches.VoiceManager = VoiceManager;

            _harmony = new Harmony(PluginGuid);
            DialoguePatches.Apply(_harmony);
            LoadingScreenPatches.Apply(_harmony);

            Log.LogInfo($"{PluginName} loaded. {VoiceManager.IndexedFileCount} voice files indexed.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            VoiceManager?.Destroy();
            DialoguePatches.Reset();
            LoadingScreenPatches.Reset();

            Log?.LogInfo($"{PluginName} unloaded.");
        }
    }
}
