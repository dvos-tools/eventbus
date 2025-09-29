using System;
using System.Threading;
using UnityEngine;

namespace com.DvosTools.bus.Dispatchers
{
    public class UnityDispatcher : MonoBehaviour, IDispatcher
    {
        private static UnityDispatcher? _instance;
        private static readonly object Lock = new();
        private static SynchronizationContext? _mainThreadContext;

        public static UnityDispatcher? Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Lock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject(nameof(UnityDispatcher));
                            _instance = go.AddComponent<UnityDispatcher>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            // Capture the main thread context
            _mainThreadContext = SynchronizationContext.Current;
        }

        public void Dispatch(Action? action)
        {
            if (_mainThreadContext != null)
            {
                // Use SynchronizationContext to post to the main thread
                _mainThreadContext.Post(_ => action?.Invoke(), null);
            }
            else
            {
                _mainThreadContext = SynchronizationContext.Current;
            }
        }

    }
}