using UnityEngine;

namespace com.DvosTools.bus.Runtime
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
            var logMessage = "[EventBus] === Current Handler Registry ===\n";
            foreach (var kvp in EventBus.Instance.Handlers)
                logMessage += $"[EventBus] Event: {kvp.Key.Name} -> {kvp.Value.Count} handler(s)\n";
            logMessage += "[EventBus] === End Handler Registry ===";

            Debug.Log(logMessage);
        }

        public void LogQueueStatus()
        {
            lock (EventBus.Instance.QueueLock)
            {
                var logMessage = "[EventBus] === Queue Status ===\n";
                logMessage += $"[EventBus] Queue Count: {EventBus.Instance.EventQueue.Count}\n";

                var queueArray = EventBus.Instance.EventQueue.ToArray();
                for (var i = 0; i < queueArray.Length; i++)
                {
                    var queuedEvent = queueArray[i];
                    logMessage +=
                        $"[EventBus] [{i}] {queuedEvent.EventType.Name} (Queued: {queuedEvent.QueuedAt:HH:mm:ss})\n";
                }

                logMessage += "[EventBus] === End Queue Status ===";
                Debug.Log(logMessage);
            }
        }
    }
}