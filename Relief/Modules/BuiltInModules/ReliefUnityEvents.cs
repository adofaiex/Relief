using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Relief.Modules.BuiltIn
{
    public class ReliefUnityEvents : MonoBehaviour
    {
        public EventSystem EventSystem { get; set; }
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();

        public static void Enqueue(Action action)
        {
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }

        private void Update()
        {
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                {
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting += OnApplicationQuitting;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.quitting -= OnApplicationQuitting;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EventSystem?.TriggerEvent("onSceneLoaded", scene.name, mode.ToString());
            // 同时也触发 sceneLoaded 以保持与 Patches 一致，并确保 MainClass 能捕获
            EventSystem?.TriggerEvent("sceneLoaded", new Dictionary<string, object> { { "name", scene.name }, { "mode", mode } });
        }

        private void OnApplicationQuitting()
        {
            EventSystem?.TriggerEvent("onGameClosing");
        }
    }
}