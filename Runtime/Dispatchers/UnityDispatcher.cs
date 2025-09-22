using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.DvosTools.bus.Runtime.Dispatchers
{
    public class UnityDispatcher : MonoBehaviour, IDispatcher
    {
        private static UnityDispatcher _instance;
        private readonly Queue<Action> _executionQueue = new();
        private readonly object _queueLock = new();

        public static UnityDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("UnityDispatcher");
                    _instance = go.AddComponent<UnityDispatcher>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void Dispatch(Action action)
        {
            lock (_queueLock)
            {
                _executionQueue.Enqueue(action);
            }
        }

        private void Update()
        {
            lock (_queueLock)
            {
                while (_executionQueue.Count > 0)
                {
                    var action = _executionQueue.Dequeue();
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[UnityDispatcher] Error executing action: {ex.Message}");
                    }
                }
            }
        }
    }
}