using UnityEngine;

namespace com.DvosTools.bus
{
    public static class EventBusLogger
    {
        /// <summary>
        /// Controls whether EventBus logging is enabled. 
        /// Defaults to true in Unity Editor, false in production builds.
        /// </summary>
        public static bool EnableLogging { get; set; } = 
#if UNITY_EDITOR
            true;
#else
            false;
#endif

        public static void Log(string message)
        {
            if (EnableLogging)
            {
                Debug.Log($"[EventBus] {message}");
            }
        }

        public static void LogWarning(string message)
        {
            if (EnableLogging)
            {
                Debug.LogWarning($"[EventBus] {message}");
            }
        }

        public static void LogError(string message)
        {
            if (EnableLogging)
            {
                Debug.LogError($"[EventBus] {message}");
            }
        }
    }
}