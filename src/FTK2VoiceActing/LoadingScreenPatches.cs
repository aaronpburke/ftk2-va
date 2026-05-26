using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace FTK2VoiceActing
{
    /// <summary>
    /// Harmony patches for loading screen narrator voice acting.
    /// Patches LoadingScreenViewHelper.Initialize to play narrator audio
    /// for adventure intro text, and Hide to stop playback.
    /// </summary>
    public static class LoadingScreenPatches
    {
        internal static IVoicePlayback VoiceManager { get; set; }

        /// <summary>
        /// The NPC ID used for narrator voice files.
        /// Voice actors place narrator files under VoiceAssets/NARRATOR/
        /// </summary>
        public const string NarratorNpcId = "NARRATOR";

        /// <summary>
        /// Applies the Harmony patches for loading screen narration.
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            var initializeOriginal = RequiredMethod(
                typeof(LoadingScreenViewHelper),
                nameof(LoadingScreenViewHelper.Initialize),
                new[] { typeof(string), typeof(VisualElement) });

            var hideOriginal = RequiredMethod(
                typeof(LoadingScreenViewHelper),
                nameof(LoadingScreenViewHelper.Hide),
                new[] { typeof(Action) });

            var initPostfix = RequiredMethod(
                typeof(LoadingScreenPatches),
                nameof(InitializePostfix));

            var hidePrefix = RequiredMethod(
                typeof(LoadingScreenPatches),
                nameof(HidePrefix));

            harmony.Patch(initializeOriginal, postfix: new HarmonyMethod(initPostfix));
            harmony.Patch(hideOriginal, prefix: new HarmonyMethod(hidePrefix));

            Plugin.Log?.LogInfo($"[LoadingScreen] Patches applied: Initialize={initializeOriginal.Name}, Hide={hideOriginal.Name}");
        }

        /// <summary>
        /// Postfix patch for LoadingScreenViewHelper.Initialize.
        /// After the loading screen is set up, play the narrator audio for the adventure intro.
        /// The game uses Lang.__t(pAdventureID + "_INTRO") for the body text,
        /// so we use {ADVENTURE_ID}_INTRO as the dialogue key.
        /// </summary>
        internal static void InitializePostfix(string pAdventureID)
        {
            if (VoiceManager == null || string.IsNullOrEmpty(pAdventureID))
                return;

            string dialogueKey = GetNarratorKey(pAdventureID);
            Plugin.Log?.LogDebug($"[LoadingScreen] Playing narrator clip: {NarratorNpcId}/{dialogueKey}");
            VoiceManager.PlayVoiceClip(NarratorNpcId, dialogueKey);
        }

        /// <summary>
        /// Prefix patch for LoadingScreenViewHelper.Hide.
        /// Stops any narrator audio when the player dismisses the loading screen.
        /// </summary>
        internal static void HidePrefix()
        {
            VoiceManager?.StopCurrentClip();
        }

        /// <summary>
        /// Constructs the narrator dialogue key from an adventure ID.
        /// E.g., "STORY_1_1" -> "STORY_1_1_INTRO"
        /// </summary>
        public static string GetNarratorKey(string adventureId)
        {
            if (string.IsNullOrEmpty(adventureId))
                return null;

            return adventureId + "_INTRO";
        }

        /// <summary>
        /// Resets internal state. Used for cleanup and testing.
        /// </summary>
        internal static void Reset()
        {
            VoiceManager = null;
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
