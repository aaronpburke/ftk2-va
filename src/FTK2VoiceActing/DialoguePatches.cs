using HarmonyLib;
using System;
using System.Reflection;

namespace FTK2VoiceActing
{
    /// <summary>
    /// Harmony patches for NPC dialogue voice acting.
    /// Patches RenderEmitter to track the current speaker and
    /// RenderSay to trigger voice clip playback.
    /// </summary>
    public static class DialoguePatches
    {
        /// <summary>
        /// Tracks the current NPC emitter ID. Set by the RenderEmitter patch
        /// and consumed by the RenderSay patch.
        /// </summary>
        internal static string CurrentEmitter { get; set; }

        /// <summary>
        /// Reference to the voice playback service, set during plugin initialization.
        /// </summary>
        internal static IVoicePlayback VoiceManager { get; set; }

        /// <summary>
        /// Applies the Harmony patches for dialogue voice acting.
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            var renderEmitterOriginal = RequiredMethod(
                typeof(DialogueViewHelper),
                nameof(DialogueViewHelper.RenderEmitter),
                new[] { typeof(string), typeof(bool) });

            var renderSayOriginal = RequiredMethod(
                typeof(DialogueViewHelper),
                nameof(DialogueViewHelper.RenderSay),
                new[] { typeof(string), typeof(bool) });

            var deinitializeOriginal = RequiredMethod(
                typeof(DialogueViewHelper),
                nameof(DialogueViewHelper.Deinitialize));

            var emitterPrefix = RequiredMethod(
                typeof(DialoguePatches),
                nameof(RenderEmitterPrefix));

            var sayPrefix = RequiredMethod(
                typeof(DialoguePatches),
                nameof(RenderSayPrefix));

            var deinitPrefix = RequiredMethod(
                typeof(DialoguePatches),
                nameof(DeinitializePrefix));

            harmony.Patch(renderEmitterOriginal, prefix: new HarmonyMethod(emitterPrefix));
            harmony.Patch(renderSayOriginal, prefix: new HarmonyMethod(sayPrefix));
            harmony.Patch(deinitializeOriginal, prefix: new HarmonyMethod(deinitPrefix));

            Plugin.Log?.LogInfo($"[Dialogue] Patches applied: RenderEmitter, RenderSay, Deinitialize");
        }

        /// <summary>
        /// Prefix patch for DialogueViewHelper.RenderEmitter.
        /// Captures the NPC ID before the original method runs.
        /// </summary>
        internal static void RenderEmitterPrefix(string pValue, bool pDoTranslate)
        {
            Plugin.Log?.LogInfo($"[Dialogue] RenderEmitterPrefix: pValue='{pValue}', pDoTranslate={pDoTranslate}");
            if (!string.IsNullOrEmpty(pValue))
            {
                // When pDoTranslate is true, pValue is the raw NPC ID (e.g., "NPC_BARMAID")
                // When pDoTranslate is false, pValue is an already-translated display name
                // We want the raw NPC ID for file lookup
                if (pDoTranslate)
                {
                    CurrentEmitter = pValue;
                }
            }
        }

        /// <summary>
        /// Prefix patch for DialogueViewHelper.RenderSay.
        /// Plays the matching voice clip when a dialogue line is displayed.
        /// The game pre-translates SAY values before they reach RenderSay,
        /// so pValue may be translated text even when pDoTranslate is true.
        /// We first try a direct key match, then fall back to reverse-lookup
        /// through the game's localization system.
        /// </summary>
        internal static void RenderSayPrefix(string pValue, bool pDoTranslate)
        {
            if (VoiceManager == null || string.IsNullOrEmpty(pValue))
                return;

            string emitter = CurrentEmitter;
            if (string.IsNullOrEmpty(emitter))
                return;

            // Try direct match first (pValue might be a raw dialogue key)
            if (VoiceManager.HasVoiceClip(emitter, pValue))
            {
                Plugin.Log?.LogInfo($"[Dialogue] Direct key match: {emitter}/{pValue}");
                VoiceManager.PlayVoiceClip(emitter, pValue);
                return;
            }

            // The game pre-translates SAY values in the dialogue JSON before
            // they reach RenderSay, so pValue is typically the translated text.
            // Reverse-lookup through the game's localization to find the key.
            string matchedKey = VoiceManager.FindKeyByTranslatedText(emitter, pValue);
            if (matchedKey != null)
            {
                Plugin.Log?.LogInfo($"[Dialogue] Reverse translation match: {emitter}/{matchedKey}");
                VoiceManager.PlayVoiceClip(emitter, matchedKey);
                return;
            }

            if (Plugin.Log != null)
                Plugin.Log.LogDebug($"[Dialogue] No voice clip match for {emitter}, text='{pValue?.Substring(0, System.Math.Min(pValue?.Length ?? 0, 50))}'");
        }

        /// <summary>
        /// Prefix patch for DialogueViewHelper.Deinitialize.
        /// Stops any playing voice clip and resets emitter tracking when dialogue closes.
        /// </summary>
        internal static void DeinitializePrefix()
        {
            VoiceManager?.StopCurrentClip();
            Reset();
        }

        /// <summary>
        /// Resets internal state. Useful for testing and cleanup.
        /// </summary>
        internal static void Reset()
        {
            CurrentEmitter = null;
        }

        private static MethodInfo RequiredMethod(Type type, string methodName, Type[] parameters = null)
        {
            var method = AccessTools.Method(type, methodName, parameters);
            if (method == null)
                throw new MissingMethodException($"Could not find Harmony patch target: {type.FullName}.{methodName}");

            return method;
        }
    }
}
