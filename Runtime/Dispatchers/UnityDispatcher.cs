using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.DvosTools.bus.Runtime.Dispatchers
{
    public class UnityDispatcher : MonoBehaviour, IDispatcher
    {
        private static UnityDispatcher _instance;
        private readonly Queue<Action> _executionQueue = new();

        public static UnityDispatcher Instance =>
            _instance ??= new GameObject(nameof(UnityDispatcher)).AddComponent<UnityDispatcher>();

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
            if (!Application.isPlaying)
            {
                action?.Invoke();
                return;
            }

            _executionQueue.Enqueue(action);
        }

        private void Update()
        {
            while (_executionQueue.Count > 0)
            {
                var action = _executionQueue.Dequeue();
                action?.Invoke();
            }
        }
    }
}