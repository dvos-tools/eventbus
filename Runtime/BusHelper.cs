using UnityEngine;

namespace com.DvosTools.bus
{
    public class BusHelper : MonoBehaviour
    {
        private static BusHelper _instance;

        public static BusHelper Instance =>
            _instance ??= new GameObject(nameof(BusHelper)).AddComponent<BusHelper>();

        private void Awake()
        {
            if (_instance != null) return;
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LogAllHandlers()
        {
            // Per-T handler registry is internal post-refactor; expose only the queue summary.
            var logMessage = "[EventBus] === Current Handler Registry ===\n";
            logMessage += "[EventBus] (per-T registry is internal; use EventBus.GetHandlerCount<T>() / HasHandlers<T>() to inspect)\n";
            logMessage += "[EventBus] === End Handler Registry ===";
            Debug.Log(logMessage);
        }

        public void LogQueueStatus()
        {
            var logMessage = "[EventBus] === Queue Status ===\n";
            logMessage += $"[EventBus] Total Queued: {EventBus.GetQueueCount()}\n";
            logMessage += $"[EventBus] Total Buffered: {EventBus.GetTotalBufferedEventCount()}\n";
            logMessage += "[EventBus] === End Queue Status ===";
            Debug.Log(logMessage);
        }

        /// <summary>
        /// Cleans up the BusHelper resources.
        /// This can be called manually or is automatically called by Unity's OnDestroy.
        /// </summary>
        public void Cleanup()
        {
            _instance = null;
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
